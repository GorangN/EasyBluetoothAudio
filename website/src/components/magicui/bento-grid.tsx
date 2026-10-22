import type { ComponentPropsWithoutRef, ReactNode } from "react";
import "./magicui.css";

interface BentoGridProps extends ComponentPropsWithoutRef<"div"> {
  children: ReactNode;
}

interface BentoCardProps extends ComponentPropsWithoutRef<"article"> {
  name: string;
  description: string;
  background?: ReactNode;
  href?: string;
  cta?: string;
}

export function BentoGrid({ children, className = "", ...props }: BentoGridProps) {
  return <div className={`feature-grid ${className}`.trim()} {...props}>{children}</div>;
}

export function BentoCard({
  name,
  description,
  background,
  className = "",
  href,
  cta,
  children,
  ...props
}: BentoCardProps) {
  return (
    <article className={`feature-card ${className}`.trim()} {...props}>
      {background && <div className="feature-card-background">{background}</div>}
      <div className="feature-card-content">
        <h3>{name}</h3>
        <p>{description}</p>
        {children}
        {href && cta && <a className="feature-card-link" href={href}>{cta}<span aria-hidden="true"> →</span></a>}
      </div>
      <div className="feature-card-overlay" aria-hidden="true" />
    </article>
  );
}
