// Rebuilds wwwroot/css/tailwind.css from the theme in Views/Shared/_Theme.cshtml + every class used in Views / js / Services / Controllers.
// Run it (build-css.bat) after you add a NEW Tailwind class or change the theme. _Theme.cshtml stays the single source of truth.
const fs = require('fs'), path = require('path'), cp = require('child_process');
const root = path.resolve(__dirname, '..', '..');
const theme = fs.readFileSync(path.join(root, 'Views', 'Shared', '_Theme.cshtml'), 'utf8').replace(/^\uFEFF/, '');
const css = theme.slice(theme.indexOf('>') + 1, theme.lastIndexOf('</style>')).replace(/@@/g, '@');
const src = p => '@source "' + path.join(root, p).replace(/\\/g, '/') + '";\n';
const input = '@import "tailwindcss";\n' + src('Views') + src('wwwroot/js') + src('Services') + src('Controllers') + src('Data/chat-knowledge.json') + '\n' + css;
const tmp = path.join(__dirname, 'input.generated.css');
fs.writeFileSync(tmp, input);
const out = path.join(root, 'wwwroot', 'css', 'tailwind.css');
cp.execSync('npx @tailwindcss/cli -i "' + tmp + '" -o "' + out + '" --minify', { stdio: 'inherit', cwd: __dirname });
fs.unlinkSync(tmp);
console.log('Done ->', out);
