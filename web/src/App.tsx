import { useCallback, useEffect, useState } from 'react';
import { useGameLoop } from './hooks/useGameLoop';
import { HUD } from './ui/HUD';
import { GameOverOverlay } from './ui/GameOverOverlay';
import { ObjectInfoList } from './ui/ObjectInfoList';
import { DebugPanel, PANEL_WIDTH } from './debug/DebugPanel';
import { DateSelectorPanel } from './ui/DateSelectorPanel';
import './App.css';

const GAME_MAX_W = 1200;
const INFO_PANEL_W = 330;
const BOTTOM_PANEL_H = 0;

function App() {
  const { containerRef, gameState, ready, executeOnTopAction, executeWait, resetGame, targetingState, cancelTargeting, syncAndUpdate, modelRef, rendererRef, debugNotice } = useGameLoop();
  const [debugOpen, setDebugOpen] = useState(false);
  const [viewW, setViewW] = useState(() => window.innerWidth);

  useEffect(() => {
    const onResize = () => setViewW(window.innerWidth);
    window.addEventListener('resize', onResize);
    return () => window.removeEventListener('resize', onResize);
  }, []);

  const onDebugOpenChange = useCallback((open: boolean) => {
    setDebugOpen(open);
    setTimeout(() => window.dispatchEvent(new Event('resize')), 0);
  }, []);

  const showRight = viewW > GAME_MAX_W;
  const hasPanelContent = gameState.floorBodies.length > 0 || gameState.floorGrasses.length > 0;

  return (
    <div style={{ width: '100vw', height: '100vh', display: 'flex', alignItems: 'stretch', justifyContent: 'center' }}>
      <div style={{ width: '100%', maxWidth: GAME_MAX_W, height: '100%', display: 'flex', position: 'relative' }}>
        <div ref={containerRef} style={{ flex: 1, height: !showRight && hasPanelContent ? `calc(100% - ${BOTTOM_PANEL_H}px)` : '100%', minWidth: 0 }} />

        {ready && (
          <>
            <HUD
              state={gameState}
              onTopAction={gameState.onTopAction}
              onExecuteOnTopAction={executeOnTopAction}
              onWait={executeWait}
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
            <DateSelectorPanel currentDateSeed={gameState.dateSeed} currentDifficulty={gameState.difficulty} currentTurn={gameState.turn} gameOver={!!gameState.gameOver} />
            {gameState.gameOver && (
              <GameOverOverlay info={gameState.gameOver} dateSeed={gameState.dateSeed} difficulty={gameState.difficulty} onPlayAgain={resetGame} />
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

        {/* Bottom panel for mobile/narrow viewports */}
        {ready && !showRight && hasPanelContent && (
          <div style={{
            position: 'absolute',
            bottom: 0,
            left: 0,
            right: 0,
            height: BOTTOM_PANEL_H,
            borderTop: '1px solid #333',
            overflow: 'hidden',
          }}>
            <ObjectInfoList
              bodies={gameState.floorBodies}
              grasses={gameState.floorGrasses}
              horizontal
            />
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

      {/* Right panel for desktop (viewport wider than game area) */}
      {ready && showRight && hasPanelContent && (
        <div style={{
          position: 'fixed',
          right: 0,
          top: 0,
          bottom: 0,
          width: INFO_PANEL_W,
          overflowY: 'auto',
          overflowX: 'hidden',
          display: 'flex',
          flexDirection: 'column',
        }}>
          <ObjectInfoList
            bodies={gameState.floorBodies}
            grasses={gameState.floorGrasses}
            horizontal={false}
          />
        </div>
      )}
    </div>
  );
}

export default App;
