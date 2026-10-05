---
name: commit
description: Review the working tree and create a git commit with a well-formed message.
argument-hint: "[optional hint about what the commit is for]"
disable-model-invocation: true
allowed-tools: Bash(git status:*), Bash(git diff:*), Bash(git log:*), Bash(git add:*), Bash(git commit:*), Bash(dotnet build:*)
---

# Commit

Hint from the user (may be empty): $ARGUMENTS

1. Run `git status`, `git diff` and `git diff --staged` to see everything that changed, and `git log --oneline -10`
   to match the existing message style.
2. Check for things that must not be committed: secrets, connection strings with real passwords, `appsettings.*.local.json`,
   `logs/`, `bin/`, `obj/`, real patient data. Stop and tell the user if you find any.
3. If C# files changed, run `dotnet build` and stop if it fails.
4. Decide what belongs in this commit. If the changes cover unrelated topics, propose splitting them and ask which
   to commit first.
5. Stage the files by path (`git add <paths>`), never `git add -A` blindly.
6. Write the message:
   - Summary line: imperative mood, at most 72 characters, no trailing period (e.g. `Add sleep observation factory`).
   - Blank line, then a short body explaining *why* when that isn't obvious. Wrap at 72 characters.
7. Commit with a heredoc so line breaks survive, then show `git log --oneline -1`.
8. Never push, amend, rebase or use `--no-verify` unless the user explicitly asks.
