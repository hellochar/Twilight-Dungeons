import { EntityCard } from './ObjectInfoList';
import type { EntityInfoData } from './EntityInfoPopup';

interface EntityInfoPanelProps {
  data: EntityInfoData | null;
}

export function EntityInfoPanel({ data }: EntityInfoPanelProps) {
  if (!data) return null;

  return (
    <EntityCard
      data={{
        displayName: data.name,
        typeName: data.typeName,
        hp: data.hp,
        maxHp: data.maxHp,
      }}
      horizontal={false}
      description={data.stats}
      spriteSrc={data.spriteSrc}
    />
  );
}
