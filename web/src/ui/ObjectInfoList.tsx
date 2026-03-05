import { useRef, useLayoutEffect, type RefObject } from 'react';
import type { EntityCardData } from '../hooks/useGameLoop';
import type { GameRenderer } from '../renderer/GameRenderer';
import { getObjectInfo } from '../model/ObjectInfo';
import { spriteUrl, getSpriteImgStyle } from './spriteUrl';
import { FONT_FAMILY, FONT_FAMILY_SERIF, FontSize } from './fonts';
import { Vector2Int } from '../core/Vector2Int';

const SHOW_CONNECTOR_LINES = true;
// const SHOW_CONNECTOR_LINES = true;

const SHOW_ADJACENT = false;       // entities within Chebyshev distance 1 of player
const SHOW_HOVERED = true;        // entity at the mouse-hovered tile
const SHOW_ALL = false;           // all entities on the floor (overrides above)

interface ObjectInfoListProps {
  bodies: EntityCardData[];
  grasses: EntityCardData[];
  playerPos: { x: number; y: number };
  hoveredTilePos: { x: number; y: number } | null;
  horizontal: boolean;
  containerRef: RefObject<HTMLDivElement | null>;
  rendererRef: RefObject<GameRenderer | null>;
}

function chebyshev(a: { x: number; y: number }, b: { x: number; y: number }): number {
  return Math.max(Math.abs(a.x - b.x), Math.abs(a.y - b.y));
}

export function ObjectInfoList({ bodies, grasses, playerPos, hoveredTilePos, horizontal, containerRef, rendererRef }: ObjectInfoListProps) {
  const isRelevant = (e: EntityCardData) =>
    SHOW_ALL ||
    (SHOW_ADJACENT && chebyshev(playerPos, e.pos) <= 1) ||
    (SHOW_HOVERED && hoveredTilePos != null && e.pos.x === hoveredTilePos.x && e.pos.y === hoveredTilePos.y);

  const relevant = [...bodies, ...grasses].filter(isRelevant);

  const seen = new Set<string>();
  const unique = relevant.filter(e => {
    if (seen.has(e.typeName)) return false;
    seen.add(e.typeName);
    return true;
  });

  // Collect card DOM refs for connector lines
  const cardRefsMap = useRef(new Map<string, HTMLDivElement>());
  const svgRef = useRef<SVGSVGElement>(null);

  // Update SVG lines via direct DOM mutation (no setState → no re-render loop)
  useLayoutEffect(() => {
    const svg = svgRef.current;
    if (!svg) return;

    // Clear existing lines
    while (svg.firstChild) svg.removeChild(svg.firstChild);

    const renderer = rendererRef.current;
    const canvas = containerRef.current;
    if (!SHOW_CONNECTOR_LINES || !renderer || !canvas || unique.length === 0) return;

    const canvasRect = canvas.getBoundingClientRect();
    const camera = renderer.camera;

    for (const entity of unique) {
      const cardEl = cardRefsMap.current.get(entity.typeName);
      if (!cardEl) continue;

      const cardRect = cardEl.getBoundingClientRect();
      const tilePixel = camera.tileToCenterPixel(new Vector2Int(entity.pos.x, entity.pos.y));

      const ex = canvasRect.left + tilePixel.x;
      const ey = canvasRect.top + tilePixel.y;

      let cx: number, cy: number;
      if (horizontal) {
        cx = cardRect.left + cardRect.width / 2;
        cy = cardRect.top;
      } else {
        cx = cardRect.left;
        cy = cardRect.top + cardRect.height / 2;
      }

      const line = document.createElementNS('http://www.w3.org/2000/svg', 'line');
      line.setAttribute('x1', String(ex));
      line.setAttribute('y1', String(ey));
      line.setAttribute('x2', String(cx));
      line.setAttribute('y2', String(cy));
      line.setAttribute('stroke', 'rgba(255,255,255,0.35)');
      line.setAttribute('stroke-width', '1.5');
      svg.appendChild(line);
    }
  });

  if (unique.length === 0) return null;

  return (
    <>
      <div style={{
        display: 'flex',
        flexDirection: horizontal ? 'row' : 'column',
        overflowX: horizontal ? 'auto' : 'hidden',
        overflowY: horizontal ? 'hidden' : 'visible',
        marginTop: horizontal ? undefined : 'auto',
        marginBottom: horizontal ? undefined : 'auto',
        padding: horizontal ? '0 4px' : '4px 0',
        gap: 0,
      }}>
        {unique.map((e) => (
          <EntityCard
            key={e.typeName}
            data={e}
            horizontal={horizontal}
            ref={(el) => {
              if (el) cardRefsMap.current.set(e.typeName, el);
              else cardRefsMap.current.delete(e.typeName);
            }}
          />
        ))}
      </div>
      <svg ref={svgRef} style={{
        position: 'fixed',
        inset: 0,
        width: '100vw',
        height: '100vh',
        pointerEvents: 'none',
        zIndex: 50,
      }} />
    </>
  );
}

import { forwardRef } from 'react';

export const EntityCard = forwardRef<HTMLDivElement, { data: EntityCardData; horizontal: boolean }>(
  function EntityCard({ data, horizontal }, ref) {
    const info = getObjectInfo(data.typeName);
    const description = info?.description ?? '';

    return (
      <div ref={ref} style={{
        flexShrink: 0,
        width: horizontal ? 200 : undefined,
        margin: horizontal ? '8px 4px 8px 4px' : '4px 8px',
        padding: 10,
        background: 'rgba(16, 16, 28, 0.95)',
        border: '1px solid #555',
        borderRadius: 6,
        fontFamily: FONT_FAMILY,
        color: '#ddd',
        boxShadow: '0 2px 6px rgba(0,0,0,0.4)',
      }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: description ? 6 : 0 }}>
          <img
            src={spriteUrl(data.displayName)}
            alt={data.displayName}
            style={{ height: 32, width: 'auto', imageRendering: 'pixelated', flexShrink: 0, ...getSpriteImgStyle(data.displayName) }}
            onError={(e) => { (e.target as HTMLImageElement).style.display = 'none'; }}
          />
          <span style={{ fontFamily: FONT_FAMILY_SERIF, fontSize: FontSize.serifLg, color: '#fff' }}>{data.displayName}</span>
        </div>
        {description && (
          <div style={{ fontFamily: FONT_FAMILY_SERIF, fontSize: FontSize.serifSm, color: '#fff', whiteSpace: 'pre-wrap', lineHeight: 1.3 }}>
            {description}
          </div>
        )}
      </div>
    );
  }
);
