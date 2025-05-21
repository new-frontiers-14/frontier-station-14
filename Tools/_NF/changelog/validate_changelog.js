// Dependencies
const fs = require("fs");

// Use GitHub token if available
if (process.env.GITHUB_TOKEN) axios.defaults.headers.common["Authorization"] = `Bearer ${process.env.GITHUB_TOKEN}`;

// Regexes
const HeaderRegex = /^\s*(?::cl:|🆑) *([a-z0-9_\-, ]+)?\s+/im; // :cl: or 🆑 [0] followed by optional author name [1]
const EntryRegex = /^ *[*-]? *(add|remove|tweak|fix): *([^\n\r]+)\r?$/img; // * or - followed by change type [0] and change message [1]
const CommentRegex = /<!--.*?-->/gs; // HTML comments

// Main function
async function main() {
    // Read GitHub event payload
    const eventPath = process.env.GITHUB_EVENT_PATH;
    if (!eventPath) {
        console.error('GITHUB_EVENT_PATH not set.');
        process.exit(1);
    }
    const event = JSON.parse(fs.readFileSync(eventPath, 'utf8'));
    const body = event.pull_request && event.pull_request.body ? event.pull_request.body : '';

    // Remove comments from the body
    commentlessBody = body.replace(CommentRegex, '');

    // Get author
    const headerMatch = HeaderRegex.exec(commentlessBody);
    if (!headerMatch) {
        console.log("No changelog entry found.");
        return;
    }

    // Get all changes from the body
    const results = getChanges(commentlessBody);

    if (results.entries.length() <= 0)
    {
        console.log("PR has a changelog header but no valid entries. Either remove the changelog completely, or use entries of the format '- add: text', '- remove: text', '- tweak: text', or '- fix: text'.");
        return process.exit(1);
    }

    let success = true;
    results.errors.forEach((entry) => {
        console.log(`Invalid changelog entry: "${entry.type}" with message "${entry.message}"`);
        success = false;
    });

    if (!success)
        return process.exit(1);

    console.log("Changelog is valid. Entries:");
    results.errors.forEach((entry) => {
        console.log(`${entry.type}: ${entry.message}`);
    });
}

// Get all changes from the PR body
function getChanges(body) {
    const matches = [];
    const entries = [];
    const errors = [];

    for (const match of body.matchAll(EntryRegex)) {
        matches.push([match[1], match[2]]);
    }

    if (!matches)
    {
        console.log("No changelog entries found.");
        return;
    }

    // Check change types and construct changelog entry
    matches.forEach((entry) => {
        let type;

        switch (entry[0].toLowerCase()) {
            case "add":
                type = "Add";
                break;
            case "remove":
                type = "Remove";
                break;
            case "tweak":
                type = "Tweak";
                break;
            case "fix":
                type = "Fix";
                break;
            default:
                break;
        }

        if (type) {
            entries.push({
                type: type,
                message: entry[1],
            });
        } else {
            errors.push({
                type: entry[0],
                message: entry[1]
            });
        }
    });

    return {entries: entries, errors: errors};
}

// Run main
main();
