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
}

interface EntityInfoPanelProps {
  data: EntityInfoData | null;
}

export function EntityInfoPanel({ data }: EntityInfoPanelProps) {
  if (!data) return null;

  const info = getObjectInfo(data.typeName);
  const spriteSrc = data.spriteSrc ?? spriteUrl(data.name);
  const description = data.stats || info?.description || '';
  const flavorText = info?.flavorText;

  return (
    <div style={{
      fontFamily: 'CodersCrux, monospace',
      fontSize: 30,
      color: '#ddd',
      padding: '10px 12px',
    }}>
      {/* Header: sprite + name + HP */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 6 }}>
        <img
          src={spriteSrc}
          alt={data.name}
          style={{ width: 32, height: 32, imageRendering: 'pixelated', flexShrink: 0 }}
          onError={(e) => { (e.target as HTMLImageElement).style.display = 'none'; }}
        />
        <div>
          <div style={{ fontSize: 24, color: '#fff' }}>
            {data.name}
          </div>
          {data.hp != null && data.maxHp != null && (
            <div style={{ fontSize: 20, color: '#fff' }}>
              HP: {data.hp}/{data.maxHp}
            </div>
          )}
        </div>
      </div>

      {description && (
        <div style={{ whiteSpace: 'pre-wrap', lineHeight: 1.4, marginBottom: flavorText ? 8 : 0 }}>
          {description}
        </div>
      )}

      {flavorText && (
        <div style={{ fontStyle: 'italic', color: '#888', whiteSpace: 'pre-wrap', lineHeight: 1.3, fontSize: 17 }}>
          {flavorText}
        </div>
      )}
    </div>
  );
}
