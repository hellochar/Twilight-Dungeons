import { useGameLoop } from './hooks/useGameLoop';
import { HUD } from './ui/HUD';
import './App.css';

function App() {
  const { containerRef, gameState, ready } = useGameLoop();

  return (
    <div style={{ width: '100vw', height: '100vh', position: 'relative' }}>
      <div ref={containerRef} style={{ width: '100%', height: '100%' }} />
      {ready && <HUD state={gameState} />}
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
