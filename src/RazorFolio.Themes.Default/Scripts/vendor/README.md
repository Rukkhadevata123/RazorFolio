# Vendored browser libraries

## highlight.js

Vendored highlight.js **11.11.1**, BSD 3-Clause license (see `highlight-LICENSE.txt`).

`highlight.min.js` concatenates the upstream browser build followed by three additional grammars,
with a newline between files, in this order:

1. https://raw.githubusercontent.com/highlightjs/cdn-release/11.11.1/build/highlight.min.js
2. https://raw.githubusercontent.com/highlightjs/cdn-release/11.11.1/build/languages/cmake.min.js
3. https://raw.githubusercontent.com/highlightjs/cdn-release/11.11.1/build/languages/dockerfile.min.js
4. https://raw.githubusercontent.com/highlightjs/cdn-release/11.11.1/build/languages/powershell.min.js

License source: https://raw.githubusercontent.com/highlightjs/highlight.js/11.11.1/LICENSE

Concatenated bundle SHA-256: `8ef1e46c2ad0a6ca3afea954f4c83529f3b9ca36d0f7bfd229fdde887acda42a`.

The browser build contains the upstream common language set; the additional grammars cover
the existing blog's build recipes and shell examples. No automatic language detection is used.
Unspecified or unsupported languages remain plain text. Language labels come from the Markdown fence.

The bundle and license are embedded in the .NET assembly and copied to the generated site's assets.
Blog builds and readers do not need npm or a CDN. Keep the version, upstream notices and license
in sync when updating this bundle. Syntax colors are defined separately in `Styles/code.css`.

## Mermaid Tiny

Vendored **@mermaid-js/tiny 12.3.0**, MIT license (see `mermaid-LICENSE.txt`).

- Package: https://registry.npmjs.org/@mermaid-js/tiny/-/tiny-12.3.0.tgz
- Upstream: https://github.com/mermaid-js/mermaid
- Usage: https://mermaid.js.org/config/usage.html#tiny-mermaid
- `mermaid.tiny.js` is the unmodified `package/dist/mermaid.tiny.js` from that archive.
- `mermaid-LICENSE.txt` is the archive's `package/LICENSE`; bundled dependency notices remain in the script.
- Archive SHA-512 (base64): `90BO2kQvmQWj9YjBX2IsLHCw61rN69dPTn5wFfDu+b9aT7qtzfzTA3wpKWeaRAKK85refSo9How5EOeY39DWfA==`.
- Script SHA-256: `c41f5236091b83f524c4940f97a16b2b14a76f21ff84aea4fd6b66421d8a2a9a`.

The official Tiny build is a single browser script (2,852,578 bytes). It omits mindmaps,
architecture diagrams, KaTeX and ELK; this integration is verified with flowcharts and sequence
diagrams from the existing blog. Both the script and license are embedded and copied to output.
`Scripts/mermaid.js` loads the runtime from the same site's assets only when a Mermaid code
fence is present. Readers and site builds do not require npm, a CDN or a diagram server.

Keep this file byte-for-byte intact (`.gitattributes` disables newline conversion). Updating
the version requires checking the archive integrity, license, script hash and diagram regressions.

## KaTeX

Vendored **KaTeX 0.19.1**, MIT license (see `katex/LICENSE.txt`).

- Package: https://registry.npmjs.org/katex/-/katex-0.19.1.tgz
- Upstream: https://github.com/KaTeX/KaTeX
- Browser API: https://katex.org/docs/browser
- `katex/katex.min.js`, `katex/katex.min.css` and all 60 files in `katex/fonts/` are unmodified files from the archive's `package/dist/`; the license is `package/LICENSE`.
- Archive SHA-512 (base64): `jG8uKntjSEcm2EpQOh0XturpPuuC9r3Sq5HLHdsFdWZT27E672SF19QEi+of0/YeOy15lDgRhKvr5614uRrEfA==`.
- Script SHA-256: `fa140498b8cfc8302cbe5e7cb1bdbc1f952bc38b3fa258ede09e73edbf9a74bc`.
- Stylesheet SHA-256: `00e0b094171b07f58422075f469b4feac2b2f7bce1aeec943c08da4d697f4dcf`.

All resources are embedded and copied to `assets/katex/`, preserving the stylesheet's relative font URLs.
`Scripts/math.js` loads the runtime and stylesheet only on pages with Markdig math nodes;
it does not use auto-render or scan arbitrary page text for delimiters. Rendering produces HTML
and MathML, uses `trust: false`, and gives each formula an independent macro scope.
Errors retain the encoded TeX source. No CDN, npm or external renderer is required to build/read a site.
Keep upstream bytes intact (`.gitattributes` disables text conversion), and verify archive integrity,
font references, package output and real-article regressions when updating.
