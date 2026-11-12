# highlight.js

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
