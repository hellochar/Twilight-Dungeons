import { useCallback, useState } from 'react';
import { useGameLoop, type ItemSnapshot } from './hooks/useGameLoop';
import { HUD } from './ui/HUD';
import { InventoryPanel } from './ui/InventoryPanel';
import { GameOverOverlay } from './ui/GameOverOverlay';
import { EntityInfoPopup } from './ui/EntityInfoPopup';
import { DebugPanel, PANEL_WIDTH } from './debug/DebugPanel';
import './App.css';

function App() {
  const { containerRef, gameState, ready, executeItemAction, executeOnTopAction, resetGame, targetingState, cancelTargeting, syncAndUpdate, modelRef, rendererRef, debugNotice, entityInfo, setEntityInfo } = useGameLoop();
  const [debugOpen, setDebugOpen] = useState(false);

  const handleItemInfo = useCallback((item: ItemSnapshot, screenX: number, screenY: number) => {
    // Derive typeName from spriteName — items have constructor names like "ItemRedberry"
    const typeName = 'Item' + item.spriteName.charAt(0).toUpperCase() + item.spriteName.slice(1);
    setEntityInfo({
      name: item.displayName,
      typeName,
      stats: item.statsFull || undefined,
      x: screenX,
      y: screenY,
    });
  }, [setEntityInfo]);

  const onDebugOpenChange = useCallback((open: boolean) => {
    setDebugOpen(open);
    // Trigger resize after React commits the DOM change so PixiJS picks up new container width
    setTimeout(() => window.dispatchEvent(new Event('resize')), 0);
  }, []);

  return (
    <div style={{ width: '100vw', height: '100vh', display: 'flex', position: 'relative' }}>
      <div ref={containerRef} style={{ flex: 1, height: '100%', minWidth: 0 }} />
      {ready && (
        <>
          <HUD
            state={gameState}
            onTopAction={gameState.onTopAction}
            onExecuteOnTopAction={executeOnTopAction}
          />
          <InventoryPanel
            inventoryItems={gameState.inventoryItems}
            equipmentItems={gameState.equipmentItems}
            onItemAction={executeItemAction}
            onItemInfo={handleItemInfo}
            disabled={gameState.isPlayerDead}
            targetingActive={!!targetingState}
          />
          {targetingState && (
            <div style={{
              position: 'absolute',
              top: 8,
              left: '50%',
              transform: 'translateX(-50%)',
              background: 'rgba(20, 20, 32, 0.9)',
              border: '1px solid #4f4',
              borderRadius: 4,
              padding: '6px 14px',
              fontFamily: 'CodersCrux, monospace',
              fontSize: 18,
              color: '#ccc',
              pointerEvents: 'auto',
              zIndex: 10,
            }}>
              Click a highlighted target
              <button
                onClick={cancelTargeting}
                style={{
                  marginLeft: 10,
                  background: 'transparent',
                  color: '#888',
                  border: '1px solid #555',
                  borderRadius: 3,
                  padding: '2px 8px',
                  fontFamily: 'CodersCrux, monospace',
                  fontSize: 17,
                  cursor: 'pointer',
                }}
              >
                Cancel
              </button>
            </div>
          )}
          {entityInfo && (
            <EntityInfoPopup data={entityInfo} onClose={() => setEntityInfo(null)} />
          )}
          {gameState.gameOver && (
            <GameOverOverlay info={gameState.gameOver} dateSeed={gameState.dateSeed} onPlayAgain={resetGame} />
          )}
          {debugNotice && (
            <div style={{
              position: 'absolute',
              top: 8,
              right: debugOpen ? PANEL_WIDTH + 8 : 8,
              background: 'rgba(20, 20, 32, 0.9)',
              border: '1px solid #6cf',
              borderRadius: 4,
              padding: '6px 12px',
              fontFamily: 'CodersCrux, monospace',
              fontSize: 17,
              color: '#6cf',
              pointerEvents: 'none',
              zIndex: 15,
              transition: 'opacity 0.3s',
            }}>
              {debugNotice}
            </div>
          )}
        </>
      )}
      {!ready && (
        <div style={{
          position: 'absolute',
          inset: 0,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          color: '#888',
          fontFamily: 'CodersCrux, monospace',
        }}>
          Loading...
        </div>
      )}
      {import.meta.env.DEV && ready && (
        <DebugPanel
          syncAndUpdate={syncAndUpdate}
          modelRef={modelRef}
          rendererRef={rendererRef}
          onOpenChange={onDebugOpenChange}
        />
      )}
    </div>
  );
}

export default App;
