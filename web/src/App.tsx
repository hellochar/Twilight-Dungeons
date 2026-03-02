import { useGameLoop } from './hooks/useGameLoop';
import { HUD } from './ui/HUD';
import { InventoryPanel } from './ui/InventoryPanel';
import { GameOverOverlay } from './ui/GameOverOverlay';
import { DebugPanel } from './debug/DebugPanel';
import './App.css';

function App() {
  const { containerRef, gameState, ready, executeItemAction, executeOnTopAction, resetGame, targetingState, cancelTargeting, syncAndUpdate, modelRef, rendererRef } = useGameLoop();

  return (
    <div style={{ width: '100vw', height: '100vh', position: 'relative' }}>
      <div ref={containerRef} style={{ width: '100%', height: '100%' }} />
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
            disabled={!!gameState.gameOver}
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
              fontFamily: 'monospace',
              fontSize: 12,
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
                  fontFamily: 'monospace',
                  fontSize: 11,
                  cursor: 'pointer',
                }}
              >
                Cancel
              </button>
            </div>
          )}
          {gameState.gameOver && (
            <GameOverOverlay info={gameState.gameOver} onPlayAgain={resetGame} />
          )}
          {import.meta.env.DEV && (
            <DebugPanel
              syncAndUpdate={syncAndUpdate}
              modelRef={modelRef}
              rendererRef={rendererRef}
            />
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
          fontFamily: 'monospace',
        }}>
          Loading...
        </div>
      )}
    </div>
  );
}

export default App;
