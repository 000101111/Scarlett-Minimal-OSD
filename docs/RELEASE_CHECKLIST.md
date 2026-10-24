# Publication checklist

- [ ] Choose the project owner and a license for original WPF code; assess GPL-2.0 obligations of `aes70` (do not assume MIT is valid for the complete distributed application).
- [ ] Resolve tray-wide mouse-wheel hook issue; prevent accidental gain changes.
- [ ] Harden bridge gain writes: reject disconnected/unknown state; enforce configured gain ceiling in the bridge too.
- [ ] Verify startup, FC2 pairing, MAIN/ALT, MUTE/DIM, and physical volume knob on actual 16i16 4th Gen.
- [ ] Test tray double-click show/hide, FC2 relaunch and restoration after OSD exit/crash.
- [ ] Implement and test Windows onboarding/pairing for a clean new profile; bundled discovery helper currently calls `lsof`.
- [ ] Test unknown device, disconnection and reconnection, FC2 restart, and device moved to another PC.
- [ ] Run Windows build and a clean VM test with no pre-existing Node/npm deps; verify runtime prerequisites.
- [ ] Review icons/trademarks, all vendor dependencies and their redistribution notices.
- [ ] Confirm no `settings.json`, logs, `client-key.json`, serials, tokens, local paths, or private identities in git history or release ZIP.
- [ ] Add screenshot and supported hardware / tested FC2 version once independently verified.
- [ ] Decide whether portable Node.exe is desirable for release package size.
