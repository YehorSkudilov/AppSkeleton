# AppSkeleton: rules for Claude

## Merge to main when a change is finished

The owner has standing permission for this; don't wait to be asked.

- When a feature, fix or change is finished, merge it to `main` straight away. Don't wait for the owner to say
  "merge to main", and don't open a pull request unless asked.
- Before pushing: fetch and rebase onto the latest `origin/main`, and build what the change touches:
  `dotnet build AppSkeleton/AppSkeleton.csproj -f net11.0-android` (iOS needs a Mac; say so when a change is
  iOS-only and couldn't be built).
- The apps' publish workflows (BudgetBuddy, Gym Book) build against AppSkeleton's `main`, so a change only reaches
  their next builds once it's merged.
- Push with `git push origin HEAD:main`, as a fast-forward. Never force-push `main`.
- Then update the session's working branch to match.
- If a build fails, or the rebase hits conflicts that aren't clearly yours to resolve, don't merge. Say what's wrong.
- Work that's half-done stays on the working branch until it's finished.
