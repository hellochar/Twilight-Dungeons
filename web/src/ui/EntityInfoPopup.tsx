import { useEffect, useRef } from 'react';
import { getObjectInfo } from '../model/ObjectInfo';
import { spriteUrl } from './spriteUrl';

export interface EntityInfoData {
  name: string;
  /** Constructor name for ObjectInfo lookup. */
  typeName: string;
  hp?: number;
  maxHp?: number;
  /** Item stats string (from getStatsFull). */
  stats?: string;
  /** Override sprite URL (e.g. for statuses that use a different URL scheme). */
  spriteSrc?: string;
  /** Screen position to anchor popup near. */
  x: number;
  y: number;
}

interface EntityInfoPopupProps {
  data: EntityInfoData;
  onClose: () => void;
}

export function EntityInfoPopup({ data, onClose }: EntityInfoPopupProps) {
  const ref = useRef<HTMLDivElement>(null);
  const info = getObjectInfo(data.typeName);

  // Close on click outside
  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) {
        onClose();
      }
    };
    // Delay to avoid closing immediately from the same click that opened it
    const timer = setTimeout(() => document.addEventListener('mousedown', handler), 50);
    return () => {
      clearTimeout(timer);
      document.removeEventListener('mousedown', handler);
    };
  }, [onClose]);

  // Close on Escape
  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
    };
    document.addEventListener('keydown', handler);
    return () => document.removeEventListener('keydown', handler);
  }, [onClose]);

  // Position: try to place to the right and below the click point, but keep on screen
  const popupWidth = 240;
  const popupMaxHeight = 300;
  const margin = 8;
  let left = data.x + margin;
  let top = data.y + margin;

  if (left + popupWidth > window.innerWidth - margin) {
    left = data.x - popupWidth - margin;
  }
  if (top + popupMaxHeight > window.innerHeight - margin) {
    top = Math.max(margin, window.innerHeight - popupMaxHeight - margin);
  }

  const spriteSrc = data.spriteSrc ?? spriteUrl(data.name);
  const description = data.stats || info?.description || '';
  const flavorText = info?.flavorText;

  return (
    <div
      ref={ref}
      style={{
        position: 'fixed',
        left,
        top,
        width: popupWidth,
        maxHeight: popupMaxHeight,
        overflow: 'auto',
        background: 'rgba(16, 16, 28, 0.95)',
        border: '1px solid #555',
        borderRadius: 6,
        padding: 10,
        fontFamily: 'CodersCrux, monospace',
        fontSize: 18,
        color: '#ddd',
        zIndex: 100,
        boxShadow: '0 4px 12px rgba(0,0,0,0.6)',
      }}
    >
      {/* Header: sprite + name + HP */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 8 }}>
        <img
          src={spriteSrc}
          alt={data.name}
          style={{
            width: 32,
            height: 32,
            imageRendering: 'pixelated',
          }}
          onError={(e) => { (e.target as HTMLImageElement).style.display = 'none'; }}
        />
        <div>
          <div style={{ fontWeight: 'bold', fontSize: 20, color: '#fff' }}>
            {data.name}
          </div>
          {data.hp != null && data.maxHp != null && (
            <div style={{ fontSize: 17, color: '#aaa' }}>
              HP: {data.hp}/{data.maxHp}
            </div>
          )}
        </div>
      </div>

      {/* Description */}
      {description && (
        <div style={{ whiteSpace: 'pre-wrap', lineHeight: 1.4, marginBottom: flavorText ? 8 : 0 }}>
          {description}
        </div>
      )}

      {/* Flavor text */}
      {flavorText && (
        <div style={{ fontStyle: 'italic', color: '#888', whiteSpace: 'pre-wrap', lineHeight: 1.3, fontSize: 17 }}>
          {flavorText}
        </div>
      )}
    </div>
  );
}
