# Third-party components & distribution notes

1. **Focusrite Control API** (`bridge/src/` and `bridge/package.json`): adapted from https://github.com/alexanderdalesio/focusrite-control-2-api — **MIT**, © 2026 alexanderdalesio. Full original notice is copied under `third_party/focusrite-control-2-api-LICENSE.txt`. Modifications are present in `bridge/bridge.mjs` and integration code.
2. **aes70** (npm package, ^2.0.19): **GPL-2.0** according to its published package metadata. A binary release that bundles it should include its license text and satisfy GPL source / redistribution requirements. Its license is already included inside the npm package files; review compatibility and fulfill obligations before publishing your final official release.
3. Other npm dependencies: `jsqr`, `libsodium-wrappers`, `pngjs`, `usb`, `ws` (and transitive packages) have their own licenses. Inspect the exact installed package versions and notices when preparing a release.
4. **Node.js**: bundled in the portable preview, subject to Node.js and dependency distribution terms. The preview includes its original binary unmodified.
5. **Focusrite** logos/names: third-party trademarks. The source includes custom icon assets and code that may extract the installed vendor's icon at runtime. Review artwork and trademark usage separately from software code licenses.

**Important:** The maintainer has not yet provided an explicit license for the WPF application. This package intentionally does not invent one. Do not describe this source as GPL/MIT-licensed as a whole until legal and ownership review is complete.
