---
name: EasyBluetoothAudio Web
description: A charcoal and acid-yellow landing page for the Windows Bluetooth audio receiver.
colors:
  ground: "#171a17"
  surface: "#20251f"
  raised: "#2b3229"
  ink: "#f3f5ec"
  muted: "#b9c1b3"
  accent: "#d8ec44"
  accent-ink: "#1b2217"
  accent-hover: "#e4f46b"
typography:
  display:
    fontFamily: "'Chivo Variable', 'Segoe UI', sans-serif"
    fontSize: "clamp(2.8rem, 5.5vw, 5.1rem)"
    fontWeight: 720
    lineHeight: 1.06
    letterSpacing: "-.04em"
  headline:
    fontFamily: "'Chivo Variable', 'Segoe UI', sans-serif"
    fontSize: "clamp(2rem, 3.6vw, 3.35rem)"
    fontWeight: 650
    lineHeight: 1.12
    letterSpacing: "-.035em"
  card-title:
    fontFamily: "'Chivo Variable', 'Segoe UI', sans-serif"
    fontSize: "23px"
    fontWeight: 600
    letterSpacing: "-.025em"
  body:
    fontFamily: "'Chivo Variable', 'Segoe UI', sans-serif"
    fontSize: "16px"
    fontWeight: 400
    lineHeight: 1.65
  action:
    fontFamily: "'Chivo Variable', 'Segoe UI', sans-serif"
    fontSize: "15px"
    fontWeight: 650
  supporting:
    fontFamily: "'Chivo Variable', 'Segoe UI', sans-serif"
    fontSize: "13px"
rounded:
  surface: "16px"
  action: "9px"
  inset: "8px"
  small-control: "4px"
spacing:
  section: "128px"
  section-medium: "100px"
  section-small: "64px"
  card: "32px"
  card-tablet: "24px"
  card-phone: "27px"
  feature-gap: "20px"
  action-gap: "12px"
components:
  button-primary:
    backgroundColor: "{colors.accent}"
    textColor: "{colors.accent-ink}"
    typography: "{typography.action}"
    rounded: "{rounded.action}"
    padding: "16px 22px"
  button-primary-hover:
    backgroundColor: "{colors.accent-hover}"
  button-dark:
    backgroundColor: "{colors.accent-ink}"
    textColor: "{colors.ink}"
    typography: "{typography.action}"
    rounded: "{rounded.action}"
    padding: "16px 22px"
  button-header:
    backgroundColor: "{colors.raised}"
    textColor: "{colors.ink}"
    rounded: "{rounded.inset}"
    padding: "13px 17px"
  card:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.ink}"
    rounded: "{rounded.surface}"
    padding: "{spacing.card}"
  card-accent:
    backgroundColor: "{colors.accent}"
    textColor: "{colors.accent-ink}"
    rounded: "{rounded.surface}"
    padding: "{spacing.card}"
  faq:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.ink}"
    rounded: "{rounded.action}"
  navigation:
    textColor: "{colors.ink}"
    padding: "14px 0"
---

# Design System: EasyBluetoothAudio Web

## Overview

**Creative North Star: "The desktop listening desk"**

A quiet hi-fi editorial surface uses the app's charcoal and acid-yellow identity, substantial type, and a phone-to-PC listening illustration. These are autonomous, code-led decisions documented after implementation, not an approved visual comp.

This system applies to the Astro landing page in `website/`. It does not prescribe changes to the native WPF application's controls, layout, or theme. Ground truth is `website/src/styles/global.css`, the Magic UI styles and components, and the rendered structure in `website/src/pages/index.astro`.

**Key Characteristics:**

- Green-tinted charcoal surfaces with one acid-yellow accent.
- Locally bundled Chivo typography and generous sectional spacing.
- Flat surfaces with responsive product illustrations.
- Motion restricted to the audio demonstration, button interaction, and desktop entrance.

## Colors

### Primary

Acid yellow (`accent`) marks downloads, audio connections, illustrated active states, a benefit card, and the final download band. Deep green (`accent-ink`) is its contrasting foreground. The lighter `accent-hover` belongs to the primary button interaction.

### Neutral

`ground` is the page canvas; `surface` separates the preview, cards, and FAQ rows; `raised` supports the secondary header action. `ink` carries headings and controls, while `muted` carries supporting paragraphs and compatibility information. The small illustration uses additional greens; these are illustration details rather than a general-purpose palette.

**The contrast rule.** Use deep-green text on the acid-yellow surface and retain the dark focus outline on the final download button.

## Typography

Chivo Variable is bundled locally, with Segoe UI and sans-serif fallbacks. The same family spans large editorial headings, straightforward paragraphs, and restrained labels.

Use `display` for the single hero H1 and `headline` for section H2s. Card titles use `card-title`; body paragraphs use `body`. Hero supporting copy is 17px on larger screens and 16px below 560px. Supporting labels are 13px. Body line-height is 1.65, with observed prose limits of 43–55ch; feature descriptions stop at 48ch.

Below 800px the H1 uses `clamp(2.5rem, 8vw, 4.4rem)`; below 560px it uses `clamp(2rem, 10.25vw, 2.55rem)`. Card titles become 20px below 800px. Do not enlarge tiny illustrative player labels into a general label style.

## Layout

The centered frame caps at 1248px and otherwise leaves 48px gutters. The desktop hero pairs headline and introduction in a 1.5:1 grid; features use 1.2:1, while setup, source, and FAQ use 1:1.3. Main section spacing and feature padding follow the extracted role tokens.

- At 1100px and below: gutters become 32px, section spacing becomes 100px, major two-column gaps tighten, and the final CTA stacks.
- At 800px and below: gutters become 20px, the header navigation hides while download stays visible, hero copy stacks, section spacing becomes 64px, and the phone illustration hides. Preview and feature layouts still use two columns.
- At 560px and below: features, setup, source, and FAQ stack. Download buttons span their available width. The preview shows its app and output selector vertically; speaker art and beams hide. Interactive targets retain a 44px minimum height.

The footer wraps naturally. Long installer filenames can break anywhere. The compact breakpoint simplifies illustrations while keeping the product explanation and download controls.

## Elevation & Depth

Tonal layering provides depth; cards have no drop shadows. The app illustration uses a thin outline. Magic UI download buttons alone carry a translucent white inset highlight, stronger on hover; exact shadow values live in the sidecar. The card hover overlay is deliberately subtle.

## Shapes

Large content containers use `surface` rounding. Download buttons and FAQ rows use `action`; inset toolbar shapes use `inset`. Rounded device silhouettes, circular step markers, and small status indicators belong to the listening illustration and setup sequence.

## Components

### Download buttons

Primary links use the acid-yellow treatment; the final accent band uses the dark variant. Both are real anchors with a 56px minimum height and explicit download labels. Hover lifts them 2px; active returns them to rest. Focus uses a 3px outline offset by 6px. The header variant is smaller and changes from raised charcoal to acid yellow on hover.

Magic UI shimmer runs only on hover or focus. Its 3s slide runs twice and its 6s rotating spark runs once, then both stop. Preserve the foreground text independently of these decorative layers.

### Feature cards

Magic UI Bento cards contain an illustration, H3, and paragraph. The accent card and charcoal card share their radius and layout; desktop padding reduces on tablet and is 27px on phone. A white overlay at 2.5% opacity responds to hover or focus within.

### Navigation and FAQ

Header and footer use simple text links, with accent hover or underline treatment. The skip link becomes visible on keyboard focus. FAQ rows are native `details`/`summary` controls, with a plus rotating 45 degrees when open; this interaction works without hydration.

### Listening preview

The labeled interface preview is illustrative, not a screenshot or a live Bluetooth connection. Only the speakers/headphones selector and animation toggle are interactive. Selection uses the yellow accent, `aria-pressed`, and a polite output announcement. Keep decorative controls non-interactive.

The preview starts static. User-triggered playback runs each AnimatedBeam once with 3s motion; the second beam starts 1.5s later. Signal bars use 900ms alternating motion until the preview automatically pauses after six seconds. The pause control freezes the bars and makes beams static immediately. Desktop hero arrival is a 700ms, 12px translation. Reduced-motion preference disables CSS transitions and animation, makes beams static, and restores ordinary scrolling.

## Do's and Don'ts

### Do:

- Do retain the locally bundled Chivo family and the extracted palette roles.
- Do preserve visible keyboard focus and the reduced-motion behavior.
- Do label the product illustration as a preview with no device connection.
- Do keep the mobile download action visible as layouts simplify.

### Don't:

- Don't apply this web layout system to native WPF views without a separate design task.
- Don't use decorative player controls as misleading interactive buttons.
- Don't introduce card shadows where the current system uses tonal separation.
- Don't treat small illustration labels as reusable body or control typography.
