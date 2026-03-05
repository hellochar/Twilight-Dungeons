import type { EntityCardData } from '../hooks/useGameLoop';
import { getObjectInfo } from '../model/ObjectInfo';
import { spriteUrl, getSpriteImgStyle } from './spriteUrl';

interface ObjectInfoListProps {
  bodies: EntityCardData[];
  grasses: EntityCardData[];
  horizontal: boolean;
}

export function ObjectInfoList({ bodies, grasses, horizontal }: ObjectInfoListProps) {
  // Deduplicate by typeName — show each entity type once
  const seen = new Set<string>();
  const unique = [...bodies, ...grasses].filter(e => {
    if (seen.has(e.typeName)) return false;
    seen.add(e.typeName);
    return true;
  });
  if (unique.length === 0) return null;

  return (
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
        <EntityCard key={e.typeName} data={e} horizontal={horizontal} />
      ))}
    </div>
  );
}

function EntityCard({ data, horizontal }: { data: EntityCardData; horizontal: boolean }) {
  const info = getObjectInfo(data.typeName);
  const description = info?.description ?? '';

  return (
    <div style={{
      flexShrink: 0,
      width: horizontal ? 200 : undefined,
      margin: horizontal ? '8px 4px 8px 4px' : '4px 8px',
      padding: 10,
      background: 'rgba(16, 16, 28, 0.95)',
      border: '1px solid #555',
      borderRadius: 6,
      fontFamily: 'CodersCrux, monospace',
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
        <span style={{ fontSize: 20, color: '#fff' }}>{data.displayName}</span>
      </div>
      {description && (
        <div style={{ fontSize: 16, color: '#fff', whiteSpace: 'pre-wrap', lineHeight: 1.3 }}>
          {description}
        </div>
      )}
    </div>
  );
}
