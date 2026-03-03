import { useState, useCallback } from 'react';
import type { ItemSnapshot } from '../hooks/useGameLoop';
import { spriteUrl } from './spriteUrl';

/**
 * Inventory + Equipment panel matching Unity's InventorySlotController + ItemController.
 *
 * Unity layout:
 *   Slot bg: panel-grey-inset sprite, 48×48 cells, -8px overlap
 *   Unused: RGBA(48,52,58,0.247)  In-use: RGBA(48,52,58,1.0)
 *   Item sprite: 32×32 centered in slot
 *   Stack count: bottom-right, white bold text
 */

const EQUIP_LABELS = ['Head', 'Weapon', 'Armor', 'Off', 'Feet'];
const SLOT_SIZE = 40;
const ITEM_SIZE = 30;
const SLOT_GAP = 2;

interface InventoryPanelProps {
  inventoryItems: (ItemSnapshot | null)[];
  equipmentItems: (ItemSnapshot | null)[];
  onItemAction: (source: 'inventory' | 'equipment', slot: number, action: string) => void;
  onItemInfo?: (item: ItemSnapshot, screenX: number, screenY: number) => void;
  disabled: boolean;
  targetingActive?: boolean;
}

interface SelectedSlot {
  source: 'inventory' | 'equipment';
  index: number;
}

export function InventoryPanel({ inventoryItems, equipmentItems, onItemAction, onItemInfo, disabled, targetingActive }: InventoryPanelProps) {
  const [selected, setSelected] = useState<SelectedSlot | null>(null);

  const handleSlotClick = useCallback((source: 'inventory' | 'equipment', index: number) => {
    if (disabled) return;
    const item = source === 'inventory' ? inventoryItems[index] : equipmentItems[index];
    if (!item) { setSelected(null); return; }
    setSelected(prev =>
      prev && prev.source === source && prev.index === index ? null : { source, index }
    );
  }, [disabled, inventoryItems, equipmentItems]);

  const handleAction = useCallback((action: string) => {
    if (!selected) return;
    onItemAction(selected.source, selected.index, action);
    setSelected(null);
  }, [selected, onItemAction]);

  const handleSlotContextMenu = useCallback((source: 'inventory' | 'equipment', index: number, e: React.MouseEvent) => {
    e.preventDefault();
    const item = source === 'inventory' ? inventoryItems[index] : equipmentItems[index];
    if (!item || !onItemInfo) return;
    onItemInfo(item, e.clientX, e.clientY);
  }, [inventoryItems, equipmentItems, onItemInfo]);

  const selectedItem = selected
    ? (selected.source === 'inventory' ? inventoryItems[selected.index] : equipmentItems[selected.index])
    : null;

  // Hide panel during targeting mode so map is unobstructed
  if (targetingActive) return null;

  return (
    <div style={{
      position: 'absolute',
      bottom: 0,
      left: 0,
      right: 0,
      display: 'flex',
      flexDirection: 'column',
      alignItems: 'center',
      padding: '4px 4px 2px',
      pointerEvents: 'none',
    }}>
      {/* Item popup */}
      {selectedItem && (
        <ItemPopup item={selectedItem} onAction={handleAction} onClose={() => setSelected(null)} />
      )}

      {/* Equipment row */}
      <div style={{ display: 'flex', gap: SLOT_GAP, marginBottom: SLOT_GAP, pointerEvents: 'auto' }}>
        {equipmentItems.map((item, i) => (
          <Slot
            key={`eq-${i}`}
            item={item}
            label={EQUIP_LABELS[i]}
            isSelected={selected?.source === 'equipment' && selected.index === i}
            onClick={() => handleSlotClick('equipment', i)}
            onContextMenu={(e) => handleSlotContextMenu('equipment', i, e)}
          />
        ))}
      </div>

      {/* Inventory row */}
      <div style={{ display: 'flex', gap: SLOT_GAP, flexWrap: 'wrap', justifyContent: 'center', pointerEvents: 'auto' }}>
        {inventoryItems.map((item, i) => (
          <Slot
            key={`inv-${i}`}
            item={item}
            isSelected={selected?.source === 'inventory' && selected.index === i}
            onClick={() => handleSlotClick('inventory', i)}
            onContextMenu={(e) => handleSlotContextMenu('inventory', i, e)}
          />
        ))}
      </div>
    </div>
  );
}

// ─── Slot ───

interface SlotProps {
  item: ItemSnapshot | null;
  label?: string;
  isSelected: boolean;
  onClick: () => void;
  onContextMenu?: (e: React.MouseEvent) => void;
}

function Slot({ item, label, isSelected, onClick, onContextMenu }: SlotProps) {
  // Unity: unused=0.247 alpha, in-use=1.0 alpha on the slot tint color
  const opacity = item ? 1.0 : 0.35;

  return (
    <div
      onClick={onClick}
      onContextMenu={onContextMenu}
      style={{
        width: SLOT_SIZE,
        height: SLOT_SIZE,
        position: 'relative',
        cursor: item ? 'pointer' : 'default',
        userSelect: 'none',
      }}
    >
      {/* Slot background: panel-grey-inset sprite */}
      <img
        src={`${import.meta.env.BASE_URL}sprites/panel-grey-inset.png`}
        alt=""
        draggable={false}
        style={{
          position: 'absolute',
          inset: 0,
          width: '100%',
          height: '100%',
          opacity,
          imageRendering: 'pixelated',
        }}
      />

      {/* Selection highlight */}
      {isSelected && (
        <div style={{
          position: 'absolute',
          inset: 0,
          border: '2px solid #88f',
          borderRadius: 2,
          pointerEvents: 'none',
          zIndex: 2,
        }} />
      )}

      {/* Slot label (equipment only, empty slot) */}
      {label && !item && (
        <div style={{
          position: 'absolute',
          inset: 0,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          fontSize: 11,
          color: 'rgba(255,255,255,0.3)',
          fontFamily: 'CodersCrux, monospace',
          textTransform: 'uppercase',
          zIndex: 1,
        }}>
          {label}
        </div>
      )}

      {/* Item content */}
      {item && (
        <>
          <img
            src={spriteUrl(item.displayName)}
            alt={item.displayName}
            draggable={false}
            style={{
              position: 'absolute',
              left: (SLOT_SIZE - ITEM_SIZE) / 2,
              top: (SLOT_SIZE - ITEM_SIZE) / 2,
              width: ITEM_SIZE,
              height: ITEM_SIZE,
              objectFit: 'contain',
              imageRendering: 'pixelated',
              zIndex: 1,
            }}
            onError={e => {
              const img = e.target as HTMLImageElement;
              if (!img.dataset.fallback) {
                img.dataset.fallback = '1';
                img.src = `${import.meta.env.BASE_URL}sprites/${item.spriteName}.png`;
              } else {
                img.style.display = 'none';
              }
            }}
          />

          {item.stacks != null && item.stacks > 1 && (
            <div style={stackLabelStyle}>{item.stacks}</div>
          )}
          {item.stacks == null && item.durability != null && item.maxDurability != null && item.durability < item.maxDurability && (
            <div style={stackLabelStyle}>{item.durability}/{item.maxDurability}</div>
          )}
        </>
      )}
    </div>
  );
}

const stackLabelStyle: React.CSSProperties = {
  position: 'absolute',
  bottom: 2,
  right: 3,
  fontSize: 14,
  fontFamily: 'CodersCrux, monospace',
  fontWeight: 'bold',
  color: '#fff',
  textShadow: '0 0 2px #000, 0 0 2px #000',
  lineHeight: '1',
  zIndex: 2,
};

// ─── Item Popup ───

interface ItemPopupProps {
  item: ItemSnapshot;
  onAction: (action: string) => void;
  onClose: () => void;
}

function ItemPopup({ item, onAction, onClose }: ItemPopupProps) {
  return (
    <div style={{
      background: 'rgba(20, 20, 32, 0.95)',
      border: '1px solid #444',
      borderRadius: 4,
      padding: '10px 12px',
      marginBottom: 6,
      display: 'flex',
      gap: 12,
      alignItems: 'flex-start',
      maxWidth: 320,
      pointerEvents: 'auto',
    }}>
      <div style={{
        width: 48,
        height: 48,
        flexShrink: 0,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
      }}>
        <img
          src={spriteUrl(item.displayName)}
          alt={item.displayName}
          draggable={false}
          style={{ width: 48, height: 48, objectFit: 'contain', imageRendering: 'pixelated' }}
          onError={e => {
            const img = e.target as HTMLImageElement;
            if (!img.dataset.fallback) {
              img.dataset.fallback = '1';
              img.src = `${import.meta.env.BASE_URL}sprites/${item.spriteName}.png`;
            }
          }}
        />
      </div>

      <div style={{ flex: 1, minWidth: 0 }}>
        <div style={{ fontFamily: 'CodersCrux, monospace', fontWeight: 'bold', fontSize: 21, color: '#eee' }}>
          {item.displayName}
        </div>
        <div style={{ fontFamily: 'CodersCrux, monospace', fontSize: 15, color: 'rgba(255,255,255,0.5)', marginBottom: 4 }}>
          {item.category}
        </div>
        {item.statsFull && (
          <div style={{ fontFamily: 'CodersCrux, monospace', fontSize: 17, color: '#aaa', marginBottom: 6, whiteSpace: 'pre-wrap' }}>
            {item.statsFull}
          </div>
        )}
        <div style={{ display: 'flex', gap: 4, flexWrap: 'wrap' }}>
          {[...item.methods].reverse().map(m => (
            <button
              key={m}
              onClick={() => onAction(m)}
              style={{
                background: 'rgba(48, 52, 58, 1)',
                color: '#ccc',
                border: '1px solid #555',
                borderRadius: 3,
                padding: '4px 10px',
                fontFamily: 'CodersCrux, monospace',
                fontSize: 17,
                cursor: 'pointer',
              }}
            >
              {m}
            </button>
          ))}
          <button
            onClick={onClose}
            style={{
              background: 'transparent',
              color: '#666',
              border: '1px solid #333',
              borderRadius: 3,
              padding: '4px 8px',
              fontFamily: 'CodersCrux, monospace',
              fontSize: 11,
              cursor: 'pointer',
            }}
          >
            ×
          </button>
        </div>
      </div>
    </div>
  );
}
