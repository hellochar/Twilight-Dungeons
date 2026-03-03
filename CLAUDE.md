# General
- In the web port, match all aspects of the Unity behavior as concisely and closely as possible. Use exact colors. ALWAYS begin by referencing the Unity implementation. Fully read relevant pieces from the original Unity project before beginning. Your goal is an exact faithful port for all content and look. Exactly replicate the animations, colors, images, sizes, timings, layouts, behavior, logic.
- After any TypeScript changes, run `npm run typecheck` (from the `web/` directory) and fix all errors before considering the task complete.
- NEVER use Bash to read, search, or edit files. Use dedicated tools instead: `find`/`ls` → Glob, `grep`/`rg` → Grep, `cat`/`head`/`tail` → Read, `sed`/`awk` → Edit, `echo >`/heredoc → Write. These handle Windows paths natively. Bash is still needed for file management (`cp`, `mv`, `rm`, `mkdir`).
- Take advantage of static typing and compiler features as much as possible.
- Do NOT add defensive null and valid checks unless the variable is used in a way that requires it.
- Be short and concise in your language.
- Only expand and explain things if asked.
- Avoid the word "Data". Use more descriptive terminology.
- Shorten summaries to just the most important code bits.
- when you are uncertain, clearly explain things you're not sure of. Double check your knowledge by accessing documentation or doing a web search.
- Do not invent abbreviations or acronyms for user facing text. Reuse terminology already established in the codebase and presented to the player. Optimize for understandability over brevity.

## Overall guidance
* Prioritize allowing the game developer to easily iterate and control numbers and content by being data driven
- Avoid adding colors unnecessarily to UI elements and text. Especially avoid super basic and overly loud colors like pure red or pure green.
