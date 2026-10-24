# Chatbot knowledge file (free, no API key, no AI)

The chatbot reads **`NexusCommunication/Data/chat-knowledge.json`**. It understands English and Roman Urdu
(spelling variants such as kese / kaise / kesay, small typing mistakes such as brodband or paymnt, and filler words),
and it answers in Roman Urdu when the question is written in Roman Urdu.
Live numbers (plans, orders, bills, stock ...) still come from the database through the role-limited lookups in
`Controllers/Api/ChatbotApiController.cs`.

* The file is re-read automatically within a few seconds after you save it (no restart needed).
* If the file is missing or broken the chatbot simply falls back to the older keyword answers.

## One entry

```json
{
  "id": "pay-now-steps",            // unique name
  "title": "How does Pay now work", // label of the "did you mean" button
  "roles": ["customer", "admin"],   // who may read it. Leave out (or "*") = everybody, visitors too.
                                    // values: public, customer, accounts, technical, retail, admin
  "keys": ["pay now", "online payment"],           // distinctive words/phrases of the topic
  "q": ["how to pay online", "online payment kaise karun", "pay now ka button kahan hai"],  // example questions - more = better
  "en": "English answer",
  "ur": "Roman Urdu answer",
  "byRole": { "customer": { "en": "...", "ur": "...", "buttons": [ ... ] } },   // optional: a different answer per role
  "route": "billing",               // optional: let the live-data engine answer (see below)
  "routeQ": "customer dues",        // optional: the wording handed to that engine
  "buttons": [ { "label": "Open My Bills", "href": "/portal#bills" },     // link (must start with /)
               { "label": "My bills", "text": "my bills" } ]              // or a message sent to the chatbot
}
```

`route` values: plans, charges, outlets, coverage, faq, complaint, orders, connections, billing, finance, invoicing,
feedback, overview, feasibility, equipment, placeorder, customers, employees, vendors, procurement, help, about,
account, greeting, thanks, plus `cheapest`, `citycheck` and `secrets`.
An entry with a `route` and its own `en`/`ur` text replaces the lead-in sentence and keeps the live cards.

## Tips
* Write 6-10 different example questions per entry, in the way people really type (Roman Urdu spellings included).
* Do not put the same question in two entries.
* The top of the file has `synonyms` (spelling variants -> one word), `stopwords` and `urduMarkers`: add new spellings there.
* A role that is not in `roles` never receives the answer: the chatbot refuses politely instead.
