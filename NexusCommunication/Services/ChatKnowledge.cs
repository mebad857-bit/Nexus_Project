using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NexusCommunication.Services;

/* ---------------------------------------------------------------------------------------------------------------
   KNOWLEDGE-FILE CHATBOT BRAIN  (no AI, no API key, no internet, free)

   Data/chat-knowledge.json  =  the "feed": hundreds of questions (English + Roman Urdu spellings) with their answers.
   This class loads that file and, for any message, finds the entry whose example questions are closest to what the
   person typed.  It understands Roman Urdu spelling variants (kese / kaise / kesay ...), small typing mistakes
   (brodband, paymnt) and extra filler words.  Matching is plain word-overlap maths (weighted Dice with fuzzy words),
   so the same question always gives the same answer.

   ROLES: every entry says who may read it ("roles"). An answer is only returned when the signed-in role is allowed;
   if the best match belongs to another desk the controller sends a polite refusal instead - never the content.
   --------------------------------------------------------------------------------------------------------------- */

public class KbButton
{
    public string Label { get; set; } = "";
    /// <summary>sent to the chatbot as a new message when the button is pressed</summary>
    public string? Text { get; set; }
    /// <summary>same-site link (must start with /)</summary>
    public string? Href { get; set; }
}

public class KbText
{
    public string? En { get; set; }
    /// <summary>Roman Urdu</summary>
    public string? Ur { get; set; }
    /// <summary>buttons for this role (instead of the entry's general buttons)</summary>
    public List<KbButton>? Buttons { get; set; }
}

public class KbEntry
{
    public string Id { get; set; } = "";
    /// <summary>short English label used on "did you mean" buttons</summary>
    public string? Title { get; set; }
    /// <summary>"*" = everybody (visitors too) or any of: public, customer, accounts, technical, retail, admin</summary>
    public string[] Roles { get; set; } = { "*" };
    /// <summary>distinctive words / phrases of the topic (a fully matching key gives a strong score)</summary>
    public string[] Keys { get; set; } = Array.Empty<string>();
    /// <summary>example questions people may type (English, Roman Urdu or mixed) - the more the better</summary>
    public string[] Q { get; set; } = Array.Empty<string>();
    public string? En { get; set; }
    /// <summary>answer in Roman Urdu (used when the person writes Roman Urdu)</summary>
    public string? Ur { get; set; }
    /// <summary>different answer per role (key = role name)</summary>
    public Dictionary<string, KbText>? ByRole { get; set; }
    /// <summary>let the live-data engine answer (plans, orders, bills ...): the topic name, or "cheapest", "citycheck", "secrets"</summary>
    public string? Route { get; set; }
    public string? RouteQ { get; set; }
    public List<KbButton>? Buttons { get; set; }

    public bool AllowedFor(string role) =>
        Roles.Length == 0 || Roles.Contains("*") || Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
}

public sealed class KbHit
{
    public KbEntry Entry { get; init; } = null!;
    public double Score { get; init; }
}

public sealed class KbMatch
{
    /// <summary>the person wrote Roman Urdu / Urdu - answer with the "ur" text</summary>
    public bool RomanUrdu { get; init; }
    /// <summary>best entry this role may read</summary>
    public KbHit? Best { get; init; }
    /// <summary>a clearly better entry exists but belongs to another desk (answer must be refused)</summary>
    public KbHit? Denied { get; init; }
    /// <summary>a few close entries this role may read ("did you mean")</summary>
    public List<KbHit> Suggestions { get; init; } = new();
    public static KbMatch Empty(bool ur) => new() { RomanUrdu = ur };
}

public class ChatKnowledge
{
    /// <summary>score from which an entry is trusted even when the older keyword engine found a topic</summary>
    public const double High = 0.70;
    /// <summary>score from which an entry answers a question the keyword engine did not understand</summary>
    public const double Low = 0.58;
    /// <summary>score from which an entry is offered as a "did you mean" suggestion</summary>
    public const double Suggest = 0.30;
    /// <summary>minimum total word weight a question must share with an example before it counts</summary>
    public const double MinEvidence = 2.2;

    private readonly string _path;
    private readonly ILogger? _log;
    private readonly object _gate = new();
    private volatile Snap _snap = Snap.Empty;
    private DateTime _stamp = DateTime.MinValue;
    private DateTime _nextCheck = DateTime.MinValue;

    public ChatKnowledge(string path, ILogger? log = null)
    {
        _path = path;
        _log = log;
        Reload();
    }

    public int Count => Fresh().Entries.Length;
    public IReadOnlyList<KbEntry> Entries => Fresh().Entries;

    /* ------------------------------------------------------------- loading ------------------------------------------------------------- */

    private Snap Fresh()
    {
        // the file is re-read automatically when it changes (so new Q&A can be added without restarting)
        var now = DateTime.UtcNow;
        if (now >= _nextCheck)
        {
            lock (_gate)
            {
                if (now >= _nextCheck)
                {
                    _nextCheck = now.AddSeconds(5);
                    try
                    {
                        if (File.Exists(_path) && File.GetLastWriteTimeUtc(_path) != _stamp) Reload();
                    }
                    catch { /* keep the old data */ }
                }
            }
        }
        return _snap;
    }

    private void Reload()
    {
        try
        {
            if (!File.Exists(_path)) { _log?.LogWarning("Chat knowledge file not found: {Path}", _path); return; }
            var stamp = File.GetLastWriteTimeUtc(_path);
            var json = File.ReadAllText(_path, Encoding.UTF8);
            var doc = JsonSerializer.Deserialize<KbFile>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            });
            if (doc == null) return;
            _snap = Snap.Build(doc);
            _stamp = stamp;
            _log?.LogInformation("Chat knowledge loaded: {Count} entries", _snap.Entries.Length);
        }
        catch (Exception ex)
        {
            // a broken file must never break the chatbot: keep the previous data (or none) and carry on
            _log?.LogWarning(ex, "Chat knowledge file could not be loaded");
        }
    }

    private sealed class KbFile
    {
        /// <summary>canonical word -> spellings / synonyms (a spelling with spaces is a phrase)</summary>
        public Dictionary<string, string[]>? Synonyms { get; set; }
        public string[]? Stopwords { get; set; }
        /// <summary>words that show the person writes Roman Urdu</summary>
        public string[]? UrduMarkers { get; set; }
        public List<KbEntry>? Entries { get; set; }
    }

    /* ------------------------------------------------------------- text helpers ------------------------------------------------------------- */

    /// <summary>The wording to show: the role's own answer if there is one, else the general one; Roman Urdu first when asked for.</summary>
    public string? TextFor(KbEntry e, string role, bool romanUrdu)
    {
        KbText? t = null;
        if (e.ByRole != null && !e.ByRole.TryGetValue(role, out t))
            e.ByRole.TryGetValue(role.ToLowerInvariant(), out t);
        string? pick(KbText? x) => x == null ? null : romanUrdu ? (x.Ur ?? x.En) : (x.En ?? x.Ur);
        return pick(t) ?? (romanUrdu ? (e.Ur ?? e.En) : (e.En ?? e.Ur));
    }

    /// <summary>Quick buttons for this role: the role's own list if it has one, else the entry's general list.</summary>
    public List<KbButton>? ButtonsFor(KbEntry e, string role)
    {
        KbText? t = null;
        if (e.ByRole != null && !e.ByRole.TryGetValue(role, out t))
            e.ByRole.TryGetValue(role.ToLowerInvariant(), out t);
        return t?.Buttons ?? e.Buttons;
    }

    public string Label(KbEntry e) => !string.IsNullOrWhiteSpace(e.Title) ? e.Title! : (e.Q.Length > 0 ? e.Q[0] : e.Id);

    /// <summary>The message to send when a "did you mean" button is pressed: an example question of the entry, in Roman Urdu when the person wrote Roman Urdu.</summary>
    public string AskText(KbEntry e, bool romanUrdu = false)
    {
        if (e.Q.Length == 0) return e.Title ?? e.Id;
        if (romanUrdu)
        {
            var s = Fresh();
            var ur = e.Q.FirstOrDefault(x => s.LooksUrdu(x));
            if (ur != null) return ur;
        }
        return e.Q[0];
    }

    /* ------------------------------------------------------------- matching ------------------------------------------------------------- */

    /// <summary>The meaningful words of a question after spelling clean-up, sorted (used to spot duplicate examples).</summary>
    public string Normalize(string raw) => string.Join(' ', Fresh().Tokens(raw).OrderBy(x => x, StringComparer.Ordinal));

    public KbMatch Match(string raw, string role)
    {
        var s = Fresh();
        var ur = s.LooksUrdu(raw);
        if (s.Entries.Length == 0) return KbMatch.Empty(ur);

        var q = s.Tokens(raw);
        if (q.Count == 0) return KbMatch.Empty(ur);

        // query token -> (vocabulary id, similarity) ; unknown words keep id -1
        var ids = new int[q.Count];
        var sim = new double[q.Count];
        double queryWeight = 0;
        for (int i = 0; i < q.Count; i++)
        {
            (ids[i], sim[i]) = s.Lookup(q[i]);
            queryWeight += ids[i] >= 0 ? s.Idf[ids[i]] : s.UnknownIdf;
        }

        var scores = new double[s.Entries.Length];
        foreach (var p in s.Patterns)
        {
            double matched = 0;
            for (int i = 0; i < ids.Length; i++)
                if (ids[i] >= 0 && Array.IndexOf(p.Tokens, ids[i]) >= 0) matched += s.Idf[ids[i]] * sim[i];
            // one common word ("time", "kitna", "order") is not enough evidence for an answer
            if (matched < MinEvidence) continue;

            double score;
            if (p.IsKey)
            {
                // a key counts only when ALL its words are present; a short key inside a long noisy sentence scores lower
                int hit = 0;
                for (int k = 0; k < p.Tokens.Length; k++)
                    for (int i = 0; i < ids.Length; i++)
                        if (ids[i] == p.Tokens[k]) { hit++; break; }
                if (hit < p.Tokens.Length) continue;
                score = 0.40 + 0.55 * Math.Min(1.0, matched / queryWeight);   // a key alone never beats an exact example question (max 0.95)
            }
            else
            {
                score = 2 * matched / (queryWeight + p.Weight);
            }
            if (score > scores[p.Entry]) scores[p.Entry] = score;
        }

        // equal scores: the entry that fetches live data wins over a plain text answer, then the earlier entry in the file
        var order = Enumerable.Range(0, scores.Length).Where(i => scores[i] > 0)
            .OrderByDescending(i => Math.Round(scores[i], 3)).ThenBy(i => s.Entries[i].Route == null ? 1 : 0).ThenBy(i => i).ToList();
        KbHit? best = null, any = null;
        var sugg = new List<KbHit>();
        foreach (var i in order)
        {
            var e = s.Entries[i];
            var hit = new KbHit { Entry = e, Score = scores[i] };
            any ??= hit;
            if (e.AllowedFor(role))
            {
                best ??= hit;
                if (sugg.Count < 3 && scores[i] >= Suggest) sugg.Add(hit);
            }
        }

        KbHit? denied = null;
        if (any != null && !any.Entry.AllowedFor(role) && any.Score >= Low && (best == null || any.Score >= best.Score + 0.08))
            denied = any;

        return new KbMatch { RomanUrdu = ur, Best = best, Denied = denied, Suggestions = sugg };
    }

    /* ------------------------------------------------------------- the prepared data ------------------------------------------------------------- */

    private sealed class Pattern
    {
        public int Entry;
        public int[] Tokens = Array.Empty<int>();
        public double Weight;
        public bool IsKey;
    }

    private sealed class Snap
    {
        public static readonly Snap Empty = new();

        public KbEntry[] Entries = Array.Empty<KbEntry>();
        public List<Pattern> Patterns = new();
        public Dictionary<string, int> Vocab = new();
        public List<string> Words = new();
        public double[] Idf = Array.Empty<double>();
        public double UnknownIdf = 3;

        private readonly Dictionary<string, string> _wordMap = new();
        private readonly List<(string From, string To)> _phrases = new();
        private readonly HashSet<string> _stop = new();
        private readonly HashSet<string> _urdu = new();
        private readonly Dictionary<string, (int, double)> _cache = new();

        private static readonly Regex Letters = new(@"[^\p{L}\p{N}]+", RegexOptions.Compiled);
        private static readonly Regex Repeats = new(@"(\p{L})\1{2,}", RegexOptions.Compiled);

        /// <summary>filler words that carry no meaning for matching</summary>
        private static readonly string[] DefaultStop =
        {
            // Roman Urdu helpers / verbs
            "hai","hain","ho","hoga","hogi","hon","hota","hoti","hote","hua","hui","hue","thi","tha","the","raha","rahi","rahe","rha","rhi","rhe",
            "hu","hun","hoon","hy","he","ha","hy","kya","kia","kiya","to","toh","tou","bhi","hi","ji","jee","zara","zra","zaraa","yaar","yar","bhai","bro","sir","madam","mam",
            "ka","ki","ke","ko","se","sy","ma","mein","men","me","main","mai","par","pe","per","aur","ya","yeh","ye","woh","wo","is","us","iss","uss","in","un",
            "kar","karo","karna","karni","karne","karun","karoon","karon","karen","kare","karta","karti","karte","kr","krna","krun","krdo","kardo","kardein","kardain","karwana","karwani","karwao",
            "sakta","sakti","sakte","sakoon","sakon","sakun","sakein","skta","skti","sakay","sakey",
            "mujhe","mjhe","mujhay","muje","mjy","mujh","humein","hamein","hame","humay","hum","ham","tum","aap","ap","apka","apki","apke","aapka","aapki","aapke","tumhara","tumhari",
            "bata","batao","batana","bataiye","bataye","batain","bataen","btao","bta","btana","dikhao","dikhaye","dikhaen","dikha","samjhao","samjha","dedo","dedein","do","dein","den","dijiye","dijye","dijiyega","de","dena",
            "please","plz","pls","kindly","can","could","would","will","shall","should","does","did","tell","me","us","give","show","explain","i","you","your","we","they","he","she","it",
            "the","a","an","is","are","am","was","were","be","been","of","for","on","at","this","that","these","those","there","here","with","about","also","just","some","any","get","want","like","know","let","so","and","or",
            "wala","wali","wale","walay","jo","jab","tab","agar","lekin","magar","phir","fir","aur","bohat","bahut","boht","bht","zyada","zada","bilkul","acha","achha","theek","thik","koi","kuch","kisi","sab","sabhi",
            "ek","aik","do","teen","hamara","hamari","hamare","humara","humari","hamain","mera","meri","mere","meray","merey",
        };

        public static Snap Build(KbFile f)
        {
            var s = new Snap();

            foreach (var w in DefaultStop) s._stop.Add(w);
            if (f.Stopwords != null) foreach (var w in f.Stopwords) s._stop.Add(w.ToLowerInvariant());
            if (f.UrduMarkers != null) foreach (var w in f.UrduMarkers) s._urdu.Add(w.ToLowerInvariant());

            // synonyms: variant -> canonical; variants with spaces are phrases
            if (f.Synonyms != null)
            {
                foreach (var (canon, variants) in f.Synonyms)
                {
                    var c = Clean(canon);
                    s._wordMap[c] = c;
                    foreach (var v in variants)
                    {
                        var vv = Clean(v);
                        if (vv.Length == 0) continue;
                        if (vv.Contains(' ')) s._phrases.Add((vv, c));
                        else s._wordMap[vv] = c;
                    }
                }
                s._phrases.Sort((a, b) => b.From.Length.CompareTo(a.From.Length));
            }

            // "my" words (mera ...) mean something for matching, so they are removed from the stop list when the file maps them
            foreach (var k in s._wordMap.Keys) s._stop.Remove(k);
            foreach (var c in s._wordMap.Values.Distinct()) s._stop.Remove(c);

            var entries = (f.Entries ?? new List<KbEntry>()).Where(e => !string.IsNullOrWhiteSpace(e.Id)).ToArray();
            s.Entries = entries;

            var raw = new List<(int Entry, List<string> Tokens, bool IsKey)>();
            for (int i = 0; i < entries.Length; i++)
            {
                foreach (var k in entries[i].Keys)
                {
                    var t = s.Tokens(k);
                    if (t.Count > 0) raw.Add((i, t, true));
                }
                foreach (var qq in entries[i].Q)
                {
                    var t = s.Tokens(qq);
                    if (t.Count > 0) raw.Add((i, t, false));
                }
            }

            // vocabulary + document frequency (documents = entries)
            var df = new Dictionary<string, HashSet<int>>();
            foreach (var (entry, tokens, _) in raw)
                foreach (var t in tokens)
                {
                    if (!df.TryGetValue(t, out var set)) df[t] = set = new HashSet<int>();
                    set.Add(entry);
                }
            var n = Math.Max(1, entries.Length);
            var idf = new List<double>();
            foreach (var (word, set) in df)
            {
                s.Vocab[word] = s.Words.Count;
                s.Words.Add(word);
                idf.Add(Math.Log(1.0 + (double)n / set.Count));
            }
            s.Idf = idf.ToArray();
            s.UnknownIdf = Math.Log(1.0 + n) * 0.8;

            foreach (var (entry, tokens, isKey) in raw)
            {
                var ids = tokens.Select(t => s.Vocab[t]).Distinct().ToArray();
                s.Patterns.Add(new Pattern { Entry = entry, Tokens = ids, IsKey = isKey, Weight = ids.Sum(x => s.Idf[x]) });
            }
            return s;
        }

        private static string Clean(string x) => Letters.Replace(x.ToLowerInvariant(), " ").Trim();

        /// <summary>true when the message is Urdu script or contains typical Roman Urdu words</summary>
        public bool LooksUrdu(string raw)
        {
            foreach (var ch in raw) if (ch >= '\u0600' && ch <= '\u06FF') return true;
            var words = Letters.Replace(raw.ToLowerInvariant(), " ").Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return words.Any(w => _urdu.Contains(w));
        }

        /// <summary>message -> canonical, de-duplicated, meaningful words</summary>
        public List<string> Tokens(string raw)
        {
            var t = " " + Clean(raw) + " ";
            t = Repeats.Replace(t, "$1");
            foreach (var (from, to) in _phrases)
            {
                var needle = " " + from + " ";
                var guard = 0;
                while (t.Contains(needle) && guard++ < 6) t = t.Replace(needle, " " + to + " ");
            }
            var list = new List<string>();
            foreach (var w0 in t.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var w = _wordMap.TryGetValue(w0, out var m) ? m : w0;
                if (w.Length == 0 || w.All(char.IsDigit)) continue;
                if (_stop.Contains(w)) continue;
                // a one-letter word is noise (except Urdu script letters)
                if (w.Length == 1 && w[0] < '\u0600') continue;
                if (!list.Contains(w)) list.Add(w);
            }
            return list;
        }

        /// <summary>exact word, or the closest word of the vocabulary within a small typing distance</summary>
        public (int Id, double Sim) Lookup(string word)
        {
            if (Vocab.TryGetValue(word, out var exact)) return (exact, 1.0);
            lock (_cache)
            {
                if (_cache.TryGetValue(word, out var c)) return c;
                var max = word.Length <= 3 ? 0 : word.Length <= 6 ? 1 : 2;
                var bestId = -1; var bestD = int.MaxValue;
                if (max > 0)
                    for (int i = 0; i < Words.Count; i++)
                    {
                        var v = Words[i];
                        if (Math.Abs(v.Length - word.Length) > max) continue;
                        if (v[0] != word[0] && v.Length <= 4) continue;   // short words must start alike
                        var d = Within(word, v, max);
                        if (d >= 0 && (d < bestD || (d == bestD && bestId >= 0 && Idf[i] > Idf[bestId]))) { bestD = d; bestId = i; }
                    }
                var res = bestId < 0 ? (-1, 0.0) : (bestId, 1.0 - 0.18 * bestD);
                if (_cache.Count > 5000) _cache.Clear();
                _cache[word] = res;
                return res;
            }
        }

        /// <summary>Levenshtein distance when it is &lt;= max, otherwise -1 (adjacent swaps count as one mistake)</summary>
        private static int Within(string a, string b, int max)
        {
            int n = a.Length, m = b.Length;
            var d = new int[n + 1, m + 1];
            for (int i = 0; i <= n; i++) d[i, 0] = i;
            for (int j = 0; j <= m; j++) d[0, j] = j;
            for (int i = 1; i <= n; i++)
            {
                var rowMin = int.MaxValue;
                for (int j = 1; j <= m; j++)
                {
                    var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    var v = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                    if (i > 1 && j > 1 && a[i - 1] == b[j - 2] && a[i - 2] == b[j - 1]) v = Math.Min(v, d[i - 2, j - 2] + 1);
                    d[i, j] = v;
                    if (v < rowMin) rowMin = v;
                }
                if (rowMin > max) return -1;
            }
            return d[n, m] <= max ? d[n, m] : -1;
        }
    }
}
