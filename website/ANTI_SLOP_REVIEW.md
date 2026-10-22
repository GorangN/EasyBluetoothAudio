# Anti Slop delivery gate

Reviewed on October 22, 2026 by the implementing agent. This is the mandatory four-block gate from the installed [miqdadbadjuber/anti-slop](https://github.com/miqdadbadjuber/anti-slop) Codex plugin, version 3.2.21, not a substitute based on the similarly named frontend skills.

## Scope and direction

Applied the core, UI, mobile-layout, human/accessibility, copywriting, and code/comment skills. During mode was selected under the user's explicit instruction to make reasonable decisions and finish autonomously; no saved global preference was changed. The same authorization supplied the visual/layout decisions rather than another interview round. The initial design direction is recorded in `../tasks/todo.md`; `../DESIGN.md` documents the resulting system.

Design read before these refinements: a free Windows audio utility for people listening at a PC; charcoal and acid yellow inherited from the native app; a hi-fi editorial composition centered on the actual phone-to-PC audio route. ENERGY 2 / RHYTHM 2 / MOTION 2. The core action is downloading the real Windows installer. The illustrative preview is explicitly labeled and starts static.

## Verification evidence

- `npm run check`: zero errors, warnings, or hints. `npm run build`: successful Astro static production build.
- `npm test`: 14 passing production-preview tests, including delayed hydration, native FAQ behavior, keyboard focus, base-path assets, metadata, bounded motion, reduced motion, and JavaScript-disabled operation.
- Responsive checks at 320, 390, 720, 768, and 1440 CSS pixels: no document/body horizontal overflow; every visible link, button, and summary target measures at least 44 by 44 pixels. The 720px reflow check represents a 1440px browser viewport at 200% zoom; this is an equivalent layout check, not a physical-device zoom claim.
- Desktop, tablet, and phone screenshots inspected. Main copy and actions remain readable, no clipping or collisions observed, and the tablet connector no longer points to a hidden phone.
- Axe: zero WCAG A/AA and best-practice violations on phone and desktop. Native keyboard activation checked for the skip link and FAQ; the download focus ring is 3px and visible.
- The plugin's own `antislop-human/contrast-check.py` passes: ink/ground 15.95:1; muted/surface 8.43:1; accent-ink/accent 12.43:1; supporting-green/accent 7.80:1; muted/window-title 6.64:1; accent/surface 11.89:1. Disabled controls are intentionally dimmed while hydration loads.
- Clicked all 17 links individually: skip, both home links, both section links, repository/navigation/source links, license links, latest release notes, support links, and all four installer links. Every navigation resolved; latest release notes redirected to v1.3.1. Each installer link started `EasyBluetoothAudioSetup.exe`; verification downloads were cancelled without executing them.
- Clicked all six FAQ controls open and closed. Switched Speakers to Headphones and back; checked the pressed state and live announcement. Played, paused, resumed, and observed the preview automatically stop after six seconds. Shimmer runs twice/once, not indefinitely. Reduced-motion disables both effects, including hovered buttons.
- The initial public deployment revealed an early-click hydration race. The delayed-module regression failed on the previous build, then passed after disabling preview controls until handlers mount. Essential download links and FAQs do not require JavaScript. Browser tests report no JavaScript console errors or failed assets.
- Product facts come from the repository, MIT license, and published release. The 14 stars and 3 forks were checked through GitHub and are explicitly dated October 22, 2026. No customer research, conversion lift, or field-performance result is claimed.

## Block 1: Hard Gate

- R-02 PASS: the rendered page-text regression rejects em dashes; none appear in the page copy.
- R-03 PASS: five viewport checks, target measurements, and three visual captures show a usable responsive layout.
- R-17 PASS: only verified, dated GitHub community counts are used; no invented downloads or user totals.
- R-18 PASS: no testimonials, customer avatars, or invented endorsements appear.
- R-23 PASS: the user's autonomous design instruction covers visual decisions; the Bluetooth identity derives from the product, and the interface diagram is honestly labeled as a preview with no device connection.
- R-24 PASS: both section destinations exist; all external navigation links were clicked and resolved.
- R-25 PASS: plugin contrast calculations and mobile/desktop axe checks pass; text is not placed over an uncontrolled background.
- R-26 PASS: all 17 links, six FAQ rows, and three preview controls have verified behavior; illustrative app controls are non-interactive and labeled by the preview context.
- R-27 PASS: this static page has no fetched-data views, submission forms, or account states. Preview controls remain disabled during module loading; static content, download, and FAQ remain available without JavaScript.
- R-28 PASS: questions address this receiver's free license, Windows/Bluetooth requirements, bundled runtime, audio route/update check, SmartScreen, and silent connections. They are product-specific documentation, not claimed customer interview results.
- R-32 PASS: native anchors/buttons/summaries preserve DOM tab order, keyboard activation and visible focus; no mouse-only menu or modal exists.
- R-33 PASS: implementation changes were written directly to source with the patch tool; no script rewrites the application or CSS.
- R-34 PASS: one complete branded dark theme ships; there is no incomplete second theme or theme toggle.
- R-35 PASS: the production build ran, browser controls were exercised, screenshots reviewed, and console/asset assertions passed. The public hydration finding received a failing-then-passing regression.
- R-36 PASS: no unverified security badges, guarantees, customer counts, latency claims, or measured conversion promises appear.
- R-37 PASS: repo-derived direction predates implementation; explicit dials predate the exact-plugin refinement. The user's autonomous-build instruction replaces the approval interview.
- R-38 PASS: features map to application source, statistics to GitHub, and downloads to the release. The visual interface is labeled illustrative rather than passed off as a live connection or screenshot.

## Block 2: Purpose-Gate

- R-01 PASS: the only gradients are Magic UI's connection beam and bounded CTA shimmer, serving audio direction and download focus; there is no gradient backdrop.
- R-04 PASS: Bluetooth, phone, speaker/headphone, download, source-code, and settings icons name actual hardware/actions; Lucide provides a consistent stroke for the app-like diagram, not generic decoration.
- R-06 PASS: locally served Chivo has a substantial, practical hi-fi character; no decorative monospace or tracked uppercase labels compete with the message.
- R-07 PASS: there is no background grid, blueprint, or dot pattern.
- R-08 PASS: download icons identify an installer action; retained external-link arrows identify GitHub destinations and the diagram arrow explains audio direction. Redundant CTA arrows were removed.
- R-09 PASS: there are no promotional capsule badges or labels repeating the headline.
- R-10 PASS: no glassmorphism or blurred overlay is used.
- R-12 PASS: tonal layers separate surfaces; only the Magic UI button's inset highlight adds a small tactile edge, rather than floating every component.
- R-13 PASS: no site-wide glow treatment is present.
- R-14 PASS: the two feature cards contrast phone-control and tray-access content, with distinct diagrams and emphasis; the underlying Magic UI Bento primitive is not a filler mosaic.
- R-19 PASS: a short desktop arrival, interaction feedback, and opt-in six-second audio illustration match MOTION 2; no perpetual entrance/stagger effects compete with reading.
- R-22 PASS: the phone/app/speakers route and tray preview explain this product specifically; no generic stock illustration appears.

## Block 3: Liveliness

- Dials PASS: ENERGY 2 / RHYTHM 2 / MOTION 2 are explicit in the design read and task record.
- Consistency PASS: strong but restrained yellow, alternating editorial composition, and bounded interaction-led motion match those dials.
- Focal points PASS: the hero download and audio route lead the first screen; benefit illustration, setup sequence, open-source explanation, and final download each lead their section.
- Whitespace PASS: 128/100/64px section spacing separates arguments; tighter controls and text spacing group related information.
- Accent PASS: one acid-yellow accent emphasizes downloads and the listening route against green-charcoal neutrals; supporting prose stays muted.
- Motif PASS: the connection path, audio waveform, device silhouettes, and two-part listening headlines maintain a specific audio identity.
- Design read PASS: the source-backed hi-fi direction was recorded before initial generation, and the exact-plugin dials were recorded before this refinement.

## Block 4: Craftsmanship and Quality Locks

- C-1 PASS: palette, type, diagrams, spacing, and interactions have written product/hierarchy reasons here and in `../DESIGN.md`.
- C-2 PASS: individual link/control click-through and production browser tests demonstrate functional completeness.
- C-3 PASS: each section answers a download decision: outcome, retained controls, setup, trust in a free offer, compatibility/objections, or download action.
- C-4 PASS: responsive, keyboard, reduced-motion, delayed-hydration, and no-JavaScript states are tested; static content does not depend on a remote API.
- C-5 PASS: proof is dated GitHub data and inspectable MIT source, not invented customer statements.
- R-05 PASS: the audio route is the central composition; the three setup steps are the actual install/pair/connect sequence. There is no logo bar, pricing grid, fake terminal, or four-column footer.
- R-11 PASS: surface/action/inset/small-control radii vary at 16/9/8/4px; only device/status shapes use other geometry for their literal subject.
- R-15 PASS: primary actions say Download for Windows or Download free; secondary links name source, release notes, or help.
- R-16 PASS: copy uses concrete audio, setup, and compatibility language without generic AI marketing superlatives.
- R-20 PASS: even without the wordmark, the phone-to-PC route, speaker silhouettes, acid-yellow audio details, and listening headlines identify an audio utility rather than a generic SaaS product.
- R-21 PASS: charcoal and acid yellow come from the native application and the listening-desk direction, not an arbitrary technology-site default.
- R-29 PASS: one green-charcoal neutral family, warm text, and one yellow accent form the core palette; extra greens belong to the device illustration.
- R-30 PASS: the hi-fi/editorial composition and literal audio diagram are not a replica of Linear, Vercel, Stripe, or Notion.
- R-31 PASS: color carries the native identity; type supplies practical hi-fi character; layout follows the download argument; spacing separates decisions; cards group two benefits; illustrations explain the audio route.

## Result

Hard Gate PASS. Purpose-Gate PASS. Liveliness PASS. Craftsmanship and Quality Locks PASS. These are implementation and review results, not a guarantee of a conversion rate or a substitute for future user testing. Public publication status is recorded separately in `../tasks/todo.md` and GitHub Actions.
