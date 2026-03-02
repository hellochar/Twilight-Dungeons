import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import type { RefObject } from 'react';
import { GameModel } from '../model/GameModel';
import type { GameRenderer } from '../renderer/GameRenderer';
import { Vector2Int } from '../core/Vector2Int';
import { Faction, TileVisibility } from '../core/types';
import { entityRegistry } from '../generator/entityRegistry';
import { allEncounters } from '../generator/Encounters';
import { StackingStatus } from '../model/Status';
import { MyRandom } from '../core/MyRandom';
import { FloorGenerator } from '../generator/FloorGenerator';
import * as AllItems from '../model/items';
import * as AllStatuses from '../model/statuses';

// ─── Types ───

interface DebugCommand {
  label: string;
  category: string;
  mode: 'instant' | 'placement';
  execute: (pos?: Vector2Int) => void;
}

interface DebugPanelProps {
  syncAndUpdate: () => void;
  modelRef: RefObject<GameModel | null>;
  rendererRef: RefObject<GameRenderer | null>;
}

// ─── Category colors ───

const CAT_COLORS: Record<string, string> = {
  Spawn: '#6cf',
  Encounter: '#f96',
  Item: '#6f6',
  Status: '#f6f',
  HP: '#f66',
  Floor: '#ff6',
  Misc: '#aaa',
};

// ─── Build commands ───

function buildCommands(
  modelRef: RefObject<GameModel | null>,
  rendererRef: RefObject<GameRenderer | null>,
  syncAndUpdate: () => void,
  regenerateWithSeed: (seed: string) => void,
): DebugCommand[] {
  const cmds: DebugCommand[] = [];

  const model = () => modelRef.current;
  const floor = () => model()?.currentFloor;
  const player = () => model()?.player;

  // ── Spawn: one per registered entity ──
  for (const name of entityRegistry.names()) {
    cmds.push({
      label: name,
      category: 'Spawn',
      mode: 'placement',
      execute: (pos) => {
        if (!pos || !floor()) return;
        const entity = entityRegistry.create(name, pos);
        if (entity) {
          floor()!.put(entity);
          syncAndUpdate();
        }
      },
    });
  }

  // ── Encounter: one per allEncounters key ──
  for (const name of Object.keys(allEncounters)) {
    cmds.push({
      label: name,
      category: 'Encounter',
      mode: 'instant',
      execute: () => {
        const f = floor();
        if (!f) return;
        allEncounters[name](f, null);
        syncAndUpdate();
      },
    });
  }

  // ── Items: one per exported item class (skip ItemHands) ──
  for (const [exportName, ItemClass] of Object.entries(AllItems)) {
    if (exportName === 'ItemHands') continue;
    cmds.push({
      label: exportName,
      category: 'Item',
      mode: 'instant',
      execute: () => {
        const p = player();
        if (!p) return;
        const item = new (ItemClass as any)();
        p.inventory.addItem(item, null);
        syncAndUpdate();
      },
    });
  }

  // ── Statuses: one per exported status class ──
  for (const [exportName, StatusClass] of Object.entries(AllStatuses)) {
    cmds.push({
      label: exportName,
      category: 'Status',
      mode: 'instant',
      execute: () => {
        const p = player();
        if (!p) return;
        const isStacking = (StatusClass as any).prototype instanceof StackingStatus;
        const status = isStacking ? new (StatusClass as any)(3) : new (StatusClass as any)();
        p.statuses.add(status);
        syncAndUpdate();
      },
    });
  }

  // ── HP commands ──
  cmds.push({
    label: 'Heal Full',
    category: 'HP',
    mode: 'instant',
    execute: () => {
      const p = player();
      if (!p) return;
      p.heal(p.maxHp);
      syncAndUpdate();
    },
  });
  cmds.push({
    label: 'Heal +4',
    category: 'HP',
    mode: 'instant',
    execute: () => {
      const p = player();
      if (!p) return;
      p.heal(4);
      syncAndUpdate();
    },
  });
  cmds.push({
    label: 'Damage -4',
    category: 'HP',
    mode: 'instant',
    execute: () => {
      const p = player();
      if (!p) return;
      p.takeDamage(4, p);
      syncAndUpdate();
    },
  });
  cmds.push({
    label: 'Max HP +4',
    category: 'HP',
    mode: 'instant',
    execute: () => {
      const p = player();
      if (!p) return;
      (p as any)._baseMaxHp += 4;
      p.heal(4);
      syncAndUpdate();
    },
  });
  cmds.push({
    label: 'Set HP to 1',
    category: 'HP',
    mode: 'instant',
    execute: () => {
      const p = player();
      if (!p) return;
      const dmg = p.hp - 1;
      if (dmg > 0) p.takeDamage(dmg, p);
      syncAndUpdate();
    },
  });

  // ── Floor commands ──
  cmds.push({
    label: 'Kill All Enemies',
    category: 'Floor',
    mode: 'instant',
    execute: () => {
      const f = floor();
      const p = player();
      if (!f || !p) return;
      const enemies = [...f.bodies].filter(
        (b) => 'faction' in b && (b as any).faction === Faction.Enemy,
      );
      for (const e of enemies) {
        if ('takeDamage' in e) (e as any).takeDamage(9999, p);
      }
      syncAndUpdate();
    },
  });
  cmds.push({
    label: 'Reveal Fog',
    category: 'Floor',
    mode: 'instant',
    execute: () => {
      const f = floor();
      if (!f) return;
      for (const pos of f.enumerateFloor()) {
        const tile = f.tiles.get(pos);
        if (tile) tile.visibility = TileVisibility.Visible;
      }
      syncAndUpdate();
    },
  });
  cmds.push({
    label: 'Regenerate Floor',
    category: 'Floor',
    mode: 'instant',
    execute: () => regenerateWithSeed(String(Date.now())),
  });
  cmds.push({
    label: 'Clear Floor',
    category: 'Floor',
    mode: 'instant',
    execute: () => {
      const f = floor();
      if (!f) return;
      (f as any).isCleared = true;
      syncAndUpdate();
    },
  });

  // ── Misc commands ──
  cmds.push({
    label: 'Teleport',
    category: 'Misc',
    mode: 'placement',
    execute: (pos) => {
      const p = player();
      const f = floor();
      if (!p || !pos || !f) return;
      (p as any)._pos = pos;
      f.bodyMoved(p, pos);
      f.recomputeVisibility();
      syncAndUpdate();
    },
  });
  cmds.push({
    label: 'Remove All Statuses',
    category: 'Misc',
    mode: 'instant',
    execute: () => {
      const p = player();
      if (!p) return;
      const statuses = [...p.statuses.list];
      for (const s of statuses) s.Remove();
      syncAndUpdate();
    },
  });
  cmds.push({
    label: 'Empty Inventory',
    category: 'Misc',
    mode: 'instant',
    execute: () => {
      const p = player();
      if (!p) return;
      for (let i = 0; i < p.inventory.capacity; i++) {
        const item = p.inventory.getAt(i);
        if (item) item.Destroy();
      }
      syncAndUpdate();
    },
  });
  cmds.push({
    label: 'Log Model',
    category: 'Misc',
    mode: 'instant',
    execute: () => {
      console.log('GameModel:', model());
      console.log('Player:', player());
      console.log('Floor:', floor());
    },
  });

  return cmds;
}

// ─── Component ───

export function DebugPanel({ syncAndUpdate, modelRef, rendererRef }: DebugPanelProps) {
  const [open, setOpen] = useState(false);
  const [filter, setFilter] = useState('');
  const [placement, setPlacement] = useState<DebugCommand | null>(null);
  const [seedInput, setSeedInput] = useState('');
  const [depthInput, setDepthInput] = useState('');
  const searchRef = useRef<HTMLInputElement>(null);

  const regenerateWithSeed = useCallback((seed: string) => {
    const renderer = rendererRef.current;
    if (!renderer) return;
    const newModel = GameModel.createDailyGame(seed);
    newModel.consumeAnimationEvents();
    modelRef.current = newModel;
    renderer.setFloor(newModel.currentFloor);
    renderer.syncToModel();
    renderer.camera.resize(
      renderer.app.screen.width,
      renderer.app.screen.height,
      newModel.currentFloor.width,
      newModel.currentFloor.height,
    );
    syncAndUpdate();
  }, [modelRef, rendererRef, syncAndUpdate]);

  const commands = useMemo(
    () => buildCommands(modelRef, rendererRef, syncAndUpdate, regenerateWithSeed),
    [modelRef, rendererRef, syncAndUpdate, regenerateWithSeed],
  );

  // Filter commands by search text
  const filtered = useMemo(() => {
    if (!filter) return commands;
    const lower = filter.toLowerCase();
    return commands.filter(
      (c) =>
        c.label.toLowerCase().includes(lower) ||
        c.category.toLowerCase().includes(lower),
    );
  }, [commands, filter]);

  // Toggle on backtick, close on Escape
  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if (e.key === '`' || e.key === '~') {
        e.preventDefault();
        setOpen((prev) => {
          if (!prev) {
            setFilter('');
            setPlacement(null);
          }
          return !prev;
        });
      }
      if (e.key === 'Escape' && open) {
        if (placement) {
          setPlacement(null);
        } else {
          setOpen(false);
        }
      }
    };
    window.addEventListener('keydown', handler);
    return () => window.removeEventListener('keydown', handler);
  }, [open, placement]);

  // Auto-focus search when opening
  useEffect(() => {
    if (open) searchRef.current?.focus();
  }, [open]);

  // Canvas click for placement mode
  useEffect(() => {
    if (!placement) return;
    const renderer = rendererRef.current;
    if (!renderer) return;
    const canvas = renderer.app.canvas as HTMLCanvasElement;
    const handler = (e: PointerEvent) => {
      const rect = canvas.getBoundingClientRect();
      const px = e.clientX - rect.left;
      const py = e.clientY - rect.top;
      const tilePos = renderer.camera.pixelToTile(px, py);
      placement.execute(tilePos);
      setPlacement(null);
    };
    canvas.addEventListener('pointerdown', handler, { once: true });
    return () => canvas.removeEventListener('pointerdown', handler);
  }, [placement, rendererRef]);

  if (!open) return null;

  const model = modelRef.current;
  const seed = model?.dateSeed ?? '?';
  const depth = model?.generatedDepth ?? model?.currentFloor?.depth ?? '?';

  // Group by category
  const groups = new Map<string, DebugCommand[]>();
  for (const cmd of filtered) {
    const list = groups.get(cmd.category) ?? [];
    list.push(cmd);
    groups.set(cmd.category, list);
  }

  const handleGenerate = () => {
    const s = seedInput.trim() || String(Date.now());
    regenerateWithSeed(s);
    setSeedInput('');
  };

  const handleGoToDepth = () => {
    const d = parseInt(depthInput, 10);
    if (isNaN(d) || d < 0 || d > 27) return;
    const renderer = rendererRef.current;
    if (!renderer) return;

    const seedStr = model?.dateSeed || new Date().toISOString().slice(0, 10);
    const djb2Hash = (str: string) => {
      let hash = 5381;
      for (let i = 0; i < str.length; i++) hash = ((hash << 5) + hash + str.charCodeAt(i)) | 0;
      return hash >>> 0;
    };
    const numSeed = djb2Hash(seedStr);
    MyRandom.setSeed(numSeed);
    const floorSeeds: number[] = [];
    for (let i = 0; i < 28; i++) floorSeeds.push(MyRandom.Range(0, 0x7fffffff));

    const generator = new FloorGenerator(floorSeeds);
    const floor = generator.generateCaveFloor(d);
    const newModel = GameModel.createAndSetMain(floor, floor.startPos);
    newModel.dateSeed = seedStr;
    newModel.generatedDepth = d;
    newModel.consumeAnimationEvents();
    modelRef.current = newModel;

    renderer.setFloor(newModel.currentFloor);
    renderer.syncToModel();
    renderer.camera.resize(
      renderer.app.screen.width,
      renderer.app.screen.height,
      newModel.currentFloor.width,
      newModel.currentFloor.height,
    );
    syncAndUpdate();
    setDepthInput('');
  };

  return (
    <div
      style={{
        position: 'absolute',
        top: 0,
        right: 0,
        width: 320,
        height: '100%',
        background: 'rgba(20, 20, 32, 0.92)',
        borderLeft: '1px solid #444',
        zIndex: 20,
        display: 'flex',
        flexDirection: 'column',
        fontFamily: 'monospace',
        fontSize: 12,
        color: '#ccc',
        pointerEvents: 'auto',
      }}
      onKeyDown={(e) => e.stopPropagation()}
    >
      {/* Seed info header */}
      <div style={{ padding: '8px 10px', borderBottom: '1px solid #333', flexShrink: 0 }}>
        <div style={{ marginBottom: 4, color: '#888', fontSize: 11 }}>
          Seed: <span style={{ color: '#fff' }}>{seed}</span>
          {' | '}Depth: <span style={{ color: '#fff' }}>{depth}</span>
        </div>
        <div style={{ display: 'flex', gap: 4, marginBottom: 4 }}>
          <input
            value={seedInput}
            onChange={(e) => setSeedInput(e.target.value)}
            placeholder="custom seed..."
            style={inputStyle}
            onKeyDown={(e) => { if (e.key === 'Enter') handleGenerate(); }}
          />
          <button onClick={handleGenerate} style={btnStyle}>Gen</button>
        </div>
        <div style={{ display: 'flex', gap: 4 }}>
          <input
            value={depthInput}
            onChange={(e) => setDepthInput(e.target.value)}
            placeholder="depth 0-27..."
            style={{ ...inputStyle, width: 90 }}
            onKeyDown={(e) => { if (e.key === 'Enter') handleGoToDepth(); }}
          />
          <button onClick={handleGoToDepth} style={btnStyle}>Go</button>
        </div>
      </div>

      {/* Placement banner */}
      {placement && (
        <div style={{
          padding: '6px 10px',
          background: 'rgba(60, 120, 60, 0.3)',
          borderBottom: '1px solid #4f4',
          flexShrink: 0,
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
        }}>
          <span>Click tile: <b style={{ color: '#4f4' }}>{placement.label}</b></span>
          <button onClick={() => setPlacement(null)} style={btnStyle}>Cancel</button>
        </div>
      )}

      {/* Search input */}
      <div style={{ padding: '6px 10px', flexShrink: 0 }}>
        <input
          ref={searchRef}
          value={filter}
          onChange={(e) => setFilter(e.target.value)}
          placeholder="Search commands..."
          style={{ ...inputStyle, width: '100%' }}
        />
      </div>

      {/* Command list */}
      <div style={{ flex: 1, overflowY: 'auto', padding: '0 4px 8px' }}>
        {[...groups.entries()].map(([category, cmds]) => (
          <div key={category}>
            <div style={{
              padding: '6px 6px 2px',
              color: CAT_COLORS[category] ?? '#aaa',
              fontSize: 11,
              fontWeight: 'bold',
              textTransform: 'uppercase',
              letterSpacing: 1,
            }}>
              {category} ({cmds.length})
            </div>
            {cmds.map((cmd) => (
              <div
                key={`${category}-${cmd.label}`}
                onClick={() => {
                  if (cmd.mode === 'placement') {
                    setPlacement(cmd);
                  } else {
                    cmd.execute();
                  }
                }}
                style={{
                  padding: '3px 6px',
                  cursor: 'pointer',
                  borderRadius: 3,
                  display: 'flex',
                  alignItems: 'center',
                  gap: 6,
                }}
                onMouseEnter={(e) => { (e.currentTarget as HTMLDivElement).style.background = 'rgba(255,255,255,0.08)'; }}
                onMouseLeave={(e) => { (e.currentTarget as HTMLDivElement).style.background = 'transparent'; }}
              >
                {cmd.mode === 'placement' && (
                  <span style={{ color: '#888', fontSize: 10 }}>[P]</span>
                )}
                <span>{cmd.label}</span>
              </div>
            ))}
          </div>
        ))}
        {filtered.length === 0 && (
          <div style={{ padding: 10, color: '#666', textAlign: 'center' }}>No matches</div>
        )}
      </div>
    </div>
  );
}

const inputStyle: React.CSSProperties = {
  flex: 1,
  background: 'rgba(255,255,255,0.06)',
  border: '1px solid #555',
  borderRadius: 3,
  padding: '3px 6px',
  fontFamily: 'monospace',
  fontSize: 11,
  color: '#ccc',
  outline: 'none',
};

const btnStyle: React.CSSProperties = {
  background: 'rgba(255,255,255,0.08)',
  border: '1px solid #555',
  borderRadius: 3,
  padding: '3px 8px',
  fontFamily: 'monospace',
  fontSize: 11,
  color: '#ccc',
  cursor: 'pointer',
};
