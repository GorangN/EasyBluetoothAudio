# Tabler outline icons

The native WPF interface uses outline geometry from [tabler/tabler-icons](https://github.com/tabler/tabler-icons), pinned to commit [`ed177a085b652fe04ec4f37ced867c94621e7840`](https://github.com/tabler/tabler-icons/tree/ed177a085b652fe04ec4f37ced867c94621e7840). The repository's latest published release when checked was v3.50.0; the commit identifier is the source pin used here.

Source SVGs are under `icons/outline/` at that commit: `bluetooth.svg`, `refresh.svg`, `settings.svg`, `x.svg`, `plus.svg`, and `download.svg`.

Original files use a 24 by 24 coordinate system, no fill, a 2-unit stroke, rounded line caps, and rounded joins. WPF preserves the paths and coordinate system, using a 1.75-unit stroke for the small desktop controls; colors follow the current native theme. The icons are vector geometry, without a browser, SVG runtime, icon font, or network dependency.

The upstream [MIT license](https://github.com/tabler/tabler-icons/blob/ed177a085b652fe04ec4f37ced867c94621e7840/LICENSE) is reproduced in Tabler-LICENSE.txt and must accompany redistributed icon geometry.
