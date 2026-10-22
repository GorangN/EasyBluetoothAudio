import { useEffect, useId, useState, type RefObject } from "react";
import { motion, useReducedMotion } from "motion/react";
import "./magicui.css";

export interface AnimatedBeamProps {
  className?: string;
  containerRef: RefObject<HTMLElement | null>;
  fromRef: RefObject<HTMLElement | null>;
  toRef: RefObject<HTMLElement | null>;
  curvature?: number;
  reverse?: boolean;
  pathColor?: string;
  pathWidth?: number;
  pathOpacity?: number;
  gradientStartColor?: string;
  gradientStopColor?: string;
  delay?: number;
  duration?: number;
  repeat?: number;
  repeatDelay?: number;
  startXOffset?: number;
  startYOffset?: number;
  endXOffset?: number;
  endYOffset?: number;
  paused?: boolean;
}

export function AnimatedBeam({
  className = "",
  containerRef,
  fromRef,
  toRef,
  curvature = 0,
  reverse = false,
  duration = 5,
  delay = 0,
  pathColor = "currentColor",
  pathWidth = 2,
  pathOpacity = 0.2,
  gradientStartColor = "#9eecb8",
  gradientStopColor = "#dcf6b8",
  repeat = Infinity,
  repeatDelay = 0,
  startXOffset = 0,
  startYOffset = 0,
  endXOffset = 0,
  endYOffset = 0,
  paused = false,
}: AnimatedBeamProps) {
  const id = useId();
  const reducedMotion = useReducedMotion();
  const [pathD, setPathD] = useState("");
  const [dimensions, setDimensions] = useState({ width: 0, height: 0 });
  const isStatic = paused || reducedMotion !== false;
  const gradientCoordinates = reverse
    ? { x1: ["90%", "-10%"], x2: ["100%", "0%"] }
    : { x1: ["10%", "110%"], x2: ["0%", "100%"] };

  useEffect(() => {
    const updatePath = () => {
      if (!containerRef.current || !fromRef.current || !toRef.current) {
        return;
      }

      const containerRect = containerRef.current.getBoundingClientRect();
      const fromRect = fromRef.current.getBoundingClientRect();
      const toRect = toRef.current.getBoundingClientRect();
      const startX = fromRect.left - containerRect.left + fromRect.width / 2 + startXOffset;
      const startY = fromRect.top - containerRect.top + fromRect.height / 2 + startYOffset;
      const endX = toRect.left - containerRect.left + toRect.width / 2 + endXOffset;
      const endY = toRect.top - containerRect.top + toRect.height / 2 + endYOffset;

      setDimensions({ width: containerRect.width, height: containerRect.height });
      setPathD(`M ${startX},${startY} Q ${(startX + endX) / 2},${startY - curvature} ${endX},${endY}`);
    };

    const observer = new ResizeObserver(updatePath);
    for (const element of [containerRef.current, fromRef.current, toRef.current]) {
      if (element) {
        observer.observe(element);
      }
    }
    window.addEventListener("resize", updatePath);
    updatePath();

    return () => {
      observer.disconnect();
      window.removeEventListener("resize", updatePath);
    };
  }, [containerRef, fromRef, toRef, curvature, startXOffset, startYOffset, endXOffset, endYOffset]);

  const stops = (
    <>
      <stop stopColor={gradientStartColor} stopOpacity="0" />
      <stop stopColor={gradientStartColor} />
      <stop offset="32.5%" stopColor={gradientStopColor} />
      <stop offset="100%" stopColor={gradientStopColor} stopOpacity="0" />
    </>
  );

  return (
    <svg
      fill="none"
      width={dimensions.width}
      height={dimensions.height}
      xmlns="http://www.w3.org/2000/svg"
      className={`magic-beam ${className}`.trim()}
      viewBox={`0 0 ${dimensions.width || 1} ${dimensions.height || 1}`}
      aria-hidden="true"
      focusable="false"
    >
      <path d={pathD} stroke={pathColor} strokeWidth={pathWidth} strokeOpacity={pathOpacity} strokeLinecap="round" />
      <path d={pathD} stroke={`url(#${id})`} strokeWidth={pathWidth} strokeLinecap="round" />
      <defs>
        {isStatic ? (
          <linearGradient id={id} gradientUnits="userSpaceOnUse" x1="0%" x2="100%" y1="0%" y2="0%">
            {stops}
          </linearGradient>
        ) : (
          <motion.linearGradient
            id={id}
            gradientUnits="userSpaceOnUse"
            initial={{ x1: "0%", x2: "0%", y1: "0%", y2: "0%" }}
            animate={{ ...gradientCoordinates, y1: ["0%", "0%"], y2: ["0%", "0%"] }}
            transition={{ delay, duration, ease: [0.16, 1, 0.3, 1], repeat, repeatDelay }}
          >
            {stops}
          </motion.linearGradient>
        )}
      </defs>
    </svg>
  );
}
