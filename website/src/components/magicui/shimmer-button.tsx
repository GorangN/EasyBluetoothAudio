import { forwardRef, type ComponentPropsWithoutRef, type CSSProperties } from "react";
import "./magicui.css";

export interface ShimmerButtonProps extends ComponentPropsWithoutRef<"a"> {
  href: string;
  shimmerColor?: string;
  shimmerSize?: string;
  borderRadius?: string;
  shimmerDuration?: string;
  background?: string;
  paused?: boolean;
}

export const ShimmerButton = forwardRef<HTMLAnchorElement, ShimmerButtonProps>(
  ({
    shimmerColor = "#ffffff",
    shimmerSize = "1px",
    shimmerDuration = "3s",
    borderRadius = "9px",
    background = "var(--accent)",
    className = "",
    children,
    style,
    paused = false,
    ...props
  }, ref) => (
    <a
      {...props}
      ref={ref}
      className={`magic-shimmer-button ${className}`.trim()}
      data-motion={paused ? "paused" : undefined}
      style={{
        "--spread": "90deg",
        "--shimmer-color": shimmerColor,
        "--radius": borderRadius,
        "--speed": shimmerDuration,
        "--cut": shimmerSize,
        "--bg": background,
        ...style,
      } as CSSProperties}
    >
      <span className="magic-shimmer-container" aria-hidden="true">
        <span className="magic-shimmer-slide"><span className="magic-shimmer-spark" /></span>
      </span>
      <span className="magic-shimmer-content">{children}</span>
      <span className="magic-shimmer-highlight" aria-hidden="true" />
      <span className="magic-shimmer-backdrop" aria-hidden="true" />
    </a>
  ),
);

ShimmerButton.displayName = "ShimmerButton";
