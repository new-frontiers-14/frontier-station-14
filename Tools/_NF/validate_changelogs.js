const fs = require('fs');

// Read GitHub event payload
const eventPath = process.env.GITHUB_EVENT_PATH;
if (!eventPath) {
  console.error('GITHUB_EVENT_PATH not set.');
  process.exit(1);
}
const event = JSON.parse(fs.readFileSync(eventPath, 'utf8'));
const prDescription = event.pull_request && event.pull_request.body ? event.pull_request.body : '';

if (!prDescription) {
  console.log('No PR description found.');
  process.exit(0);
}

// Remove HTML comments
const uncommented = prDescription.replace(/<!--[\s\S]*?-->/g, '\n');

// Find :cl: or 🆑 at the start of a line (ignoring leading whitespace)
const clHeaderMatch = uncommented.match(/^\s*(:cl:|🆑)/m);
if (!clHeaderMatch) {
  console.log('No changelog header found at the start of a line (:cl: or 🆑) outside comments. Skipping changelog entry checks.');
  process.exit(0);
}

// Extract lines after the first :cl: or 🆑 header
const lines = uncommented.split('\n');
const clHeaderIndex = lines.findIndex(line => /^\s*(:cl:|🆑)/.test(line));
const clBody = lines.slice(clHeaderIndex + 1);

// Check for at least one valid changelog entry
const validEntryRegex = /^\s*[-*]\s*(add:|remove:|tweak:|fix:)/;
const hasValidEntry = clBody.some(line => validEntryRegex.test(line));
if (!hasValidEntry) {
  console.error("PR description must contain at least one changelog entry in the format '- add:', '- remove:', '- tweak:', or '- fix:' (with a colon immediately after the keyword), outside comments.");
  process.exit(1);
}

// Check that every non-empty line after :cl: is a valid changelog entry
const invalidEntries = clBody.filter(line =>
  line.trim() !== '' && !validEntryRegex.test(line)
);

if (invalidEntries.length > 0) {
  console.error("Invalid changelog entry types found after :cl: (every line must start with only '- add:', '- remove:', '- tweak:', or '- fix:'):");
  invalidEntries.forEach(line => console.error(line));
  process.exit(1);
}

console.log('Changelog validation passed.');
