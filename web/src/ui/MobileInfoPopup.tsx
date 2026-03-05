import { useState, useRef, useLayoutEffect, type RefObject } from 'react';
import type { EntityCardData } from '../hooks/useGameLoop';
import type { GameRenderer } from '../renderer/GameRenderer';
import { EntityCard } from './ObjectInfoList';
import { getObjectInfo } from '../model/ObjectInfo';
import { Vector2Int } from '../core/Vector2Int';
import { FONT_FAMILY, FontSize } from './fonts';

interface MobileInfoPopupProps {
  bodies: EntityCardData[];
  grasses: EntityCardData[];
  hoveredTilePos: { x: number; y: number };
  containerRef: RefObject<HTMLDivElement | null>;
  rendererRef: RefObject<GameRenderer | null>;
  onClose: () => void;
}

export function MobileInfoPopup({ bodies, grasses, hoveredTilePos, containerRef, rendererRef, onClose }: MobileInfoPopupProps) {
  const [forcePosition, setForcePosition] = useState<'top' | 'bottom' | null>(null);

  // Filter entities at hovered tile
  const relevant = [...bodies, ...grasses].filter(
    e => e.pos.x === hoveredTilePos.x && e.pos.y === hoveredTilePos.y
  );
  const seen = new Set<string>();
  const unique = relevant.filter(e => {
    if (seen.has(e.typeName)) return false;
    seen.add(e.typeName);
    return true;
  });

  // Compute tile screen position for positioning logic and connector line
  const popupRef = useRef<HTMLDivElement>(null);
  const svgRef = useRef<SVGSVGElement>(null);
  const [tileScreenY, setTileScreenY] = useState<number | null>(null);

  useLayoutEffect(() => {
    const renderer = rendererRef.current;
    const canvas = containerRef.current;
    if (!renderer || !canvas) return;

    const canvasRect = canvas.getBoundingClientRect();
    const tilePixel = renderer.camera.tileToCenterPixel(new Vector2Int(hoveredTilePos.x, hoveredTilePos.y));
    const screenY = canvasRect.top + tilePixel.y;
    setTileScreenY(screenY);

    // Draw connector line
    const svg = svgRef.current;
    const popup = popupRef.current;
    if (!svg || !popup) return;
    while (svg.firstChild) svg.removeChild(svg.firstChild);

    const screenX = canvasRect.left + tilePixel.x;
    const popupRect = popup.getBoundingClientRect();
    const popupCx = popupRect.left + popupRect.width / 2;
    const popupEdgeY = popupRect.top > screenY
      ? popupRect.top
      : popupRect.bottom;

    const line = document.createElementNS('http://www.w3.org/2000/svg', 'line');
    line.setAttribute('x1', String(screenX));
    line.setAttribute('y1', String(screenY));
    line.setAttribute('x2', String(popupCx));
    line.setAttribute('y2', String(popupEdgeY));
    line.setAttribute('stroke', 'rgba(255,255,255,0.35)');
    line.setAttribute('stroke-width', '1.5');
    svg.appendChild(line);
  });

  if (unique.length === 0) return null;

  // Position: if tile is in bottom 60% of viewport, show popup at top; else bottom
  const viewportH = window.innerHeight;
  let position: 'top' | 'bottom';
  if (forcePosition) {
    position = forcePosition;
  } else {
    position = (tileScreenY != null && tileScreenY > viewportH * 0.4) ? 'top' : 'bottom';
  }

  const togglePosition = () => {
    setForcePosition(prev => {
      if (prev === 'top') return 'bottom';
      if (prev === 'bottom') return 'top';
      return position === 'top' ? 'bottom' : 'top';
    });
  };

  return (
    <>
      <div
        ref={popupRef}
        style={{
          position: 'absolute',
          left: '50%',
          transform: 'translateX(-50%)',
          ...(position === 'top'
            ? { top: 48 }
            : { bottom: 80 }),
          maxWidth: 330,
          width: 'calc(100% - 32px)',
          zIndex: 20,
          pointerEvents: 'auto',
          // background: 'rgba(16, 16, 28, 0.95)',
          // border: '1px solid #555',
          // borderRadius: 6,
          // boxShadow: '0 2px 12px rgba(0,0,0,0.6)',
          // padding: 8,
        }}
        onClick={togglePosition}
      >
        {/* Close button */}
        <button
          onClick={(e) => { e.stopPropagation(); onClose(); }}
          style={{
            position: 'absolute',
            top: 12,
            right: 12,
            background: 'transparent',
            border: 'none',
            color: '#888',
            fontFamily: FONT_FAMILY,
            fontSize: FontSize.lg,
            cursor: 'pointer',
            padding: '2px 6px',
            lineHeight: 1,
            zIndex: 1,
          }}
        >
          ✕
        </button>

        {unique.map(e => (
          <EntityCard key={e.typeName} data={e} horizontal={false} />
        ))}
      </div>

      <svg ref={svgRef} style={{
        position: 'fixed',
        inset: 0,
        width: '100vw',
        height: '100vh',
        pointerEvents: 'none',
        zIndex: 19,
      }} />
    </>
  );
}
