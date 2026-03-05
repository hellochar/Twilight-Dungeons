import { useCallback, useEffect, useState } from 'react';
import { useGameLoop } from './hooks/useGameLoop';
import { HUD } from './ui/HUD';
import { GameOverOverlay } from './ui/GameOverOverlay';
import { ObjectInfoList } from './ui/ObjectInfoList';
import { MobileInfoPopup } from './ui/MobileInfoPopup';
import { DebugPanel, PANEL_WIDTH } from './debug/DebugPanel';
import { DateSelectorPanel } from './ui/DateSelectorPanel';
import { HelpButton } from './ui/HelpPopup';
import { FONT_FAMILY, FontSize } from './ui/fonts';
import { DIFFICULTY_LABEL } from './model/GameModel';
import { isMobile } from './renderer';
import './App.css';

const GAME_MAX_W = 1200;
const INFO_PANEL_W = 330;

function App() {
  const { containerRef, gameState, ready, executeOnTopAction, executeWait, resetGame, targetingState, cancelTargeting, syncAndUpdate, modelRef, rendererRef, debugNotice, hoveredTilePos, clearHoveredTile } = useGameLoop();
  const [debugOpen, setDebugOpen] = useState(false);
  const [viewW, setViewW] = useState(() => window.innerWidth);

  useEffect(() => {
    document.title = `${gameState.dateSeed} ${DIFFICULTY_LABEL[gameState.difficulty]} | Twilight Dungeons`;
  }, [gameState.dateSeed, gameState.difficulty]);

  useEffect(() => {
    const onResize = () => setViewW(window.innerWidth);
    window.addEventListener('resize', onResize);
    return () => window.removeEventListener('resize', onResize);
  }, []);

  const onDebugOpenChange = useCallback((open: boolean) => {
    setDebugOpen(open);
    setTimeout(() => window.dispatchEvent(new Event('resize')), 0);
  }, []);

  const showRight = !isMobile();
  const hasPanelContent = gameState.floorBodies.length > 0 || gameState.floorGrasses.length > 0;

  return (
    <div style={{ width: '100vw', height: '100dvh', display: 'flex', alignItems: 'stretch', justifyContent: 'center' }}>
      <div style={{ width: '100%', maxWidth: GAME_MAX_W, height: '100%', display: 'flex', position: 'relative' }}>
        <div ref={containerRef} style={{ flex: 1, height: '100%', minWidth: 0 }} />

        {ready && (
          <>
            {/* <HelpButton /> */}
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
                fontFamily: FONT_FAMILY,
                fontSize: FontSize.md,
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
                    fontFamily: FONT_FAMILY,
                    fontSize: FontSize.md,
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
                fontFamily: FONT_FAMILY,
                fontSize: FontSize.md,
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
            fontFamily: FONT_FAMILY,
          }}>
            Loading...
          </div>
        )}

        {/* Floating info popup for mobile */}
        {ready && !showRight && hoveredTilePos && (
          <MobileInfoPopup
            bodies={gameState.floorBodies}
            grasses={gameState.floorGrasses}
            hoveredTilePos={hoveredTilePos}
            containerRef={containerRef}
            rendererRef={rendererRef}
            onClose={clearHoveredTile}
          />
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
          pointerEvents: 'none',
          overflowY: 'auto',
          overflowX: 'hidden',
          display: 'flex',
          flexDirection: 'column',
        }}>
          <ObjectInfoList
            bodies={gameState.floorBodies}
            grasses={gameState.floorGrasses}
            playerPos={gameState.playerPos}
            hoveredTilePos={hoveredTilePos}
            horizontal={false}
            containerRef={containerRef}
            rendererRef={rendererRef}
          />
        </div>
      )}
    </div>
  );
}

export default App;
