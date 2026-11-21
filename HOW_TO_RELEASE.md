# How to put Better Clock on GitHub and get the EXE built for you

You do NOT need to build the EXE yourself. GitHub builds it on a Windows machine.

## One-time setup
1. On github.com click **+ > New repository**. Name it `BetterClock`. Public. Create.
2. Click **uploading an existing file**. Drag in EVERYTHING from this folder (including the `.github` folder). Commit.

## Every release
1. Open **Releases > Draft a new release**.
2. Click **Choose a tag**, type `v1.2.0`, choose **Create new tag**.
3. Title `Better Clock v1.2.0`. Click **Publish release**.
4. Open the **Actions** tab. Wait about 2 minutes for the green tick.
5. Go back to **Releases**: `BetterClock.exe` is attached. Send people that link.

(Next versions: change `version.txt`, add `RELEASE_NOTES_v1.3.0.md`, then make a release with the tag `v1.3.0`.)

## Just testing (no public release)
**Actions > Build and release > Run workflow**. When it finishes, open the run and download the `BetterClock` file at the bottom.

## Updates
The app checks your GitHub releases. Tray menu > Check for updates (or Settings > Updates > Update now) downloads the new EXE from the latest release, installs it and restarts.
The release must be the LATEST (not a pre-release) and must have `BetterClock.exe` attached (the GitHub build does that).
GitHub builds set the repository name inside the EXE automatically.
If you build with `build.bat` on your own PC, put `yourname/BetterClock` in `repo.txt` first.

## If something goes wrong
- "Resource not accessible by integration": **Settings > Actions > General > Workflow permissions > Read and write**, then re-run.
- Build error: open the failed run in **Actions**, copy the red lines and send them to Claude.
