import { useEffect, useRef, useState } from 'react';
import { Application } from 'pixi.js';
import { GameModel } from './model/GameModel';
import { GameRenderer, Camera, SpriteManager, AnimationPlayer } from './renderer';
import './App.css';

function App() {
  const canvasRef = useRef<HTMLDivElement>(null);
  const [status, setStatus] = useState('Loading...');

  useEffect(() => {
    if (!canvasRef.current) return;

    let app: Application | null = null;

    async function init() {
      const container = canvasRef.current!;

      // Create PixiJS application
      app = new Application();
      await app.init({
        width: container.clientWidth,
        height: container.clientHeight,
        backgroundColor: 0x111118,
        resizeTo: container,
      });
      container.appendChild(app.canvas);

      // Load sprites
      const sprites = new SpriteManager();
      await sprites.load();

      // Create game model
      const model = GameModel.createTestGame();

      // Set up renderer
      const camera = new Camera();
      const renderer = new GameRenderer(app, camera, sprites);
      const _animator = new AnimationPlayer(renderer, camera);

      // Initial render
      renderer.setFloor(model.currentFloor);
      renderer.syncToModel();

      setStatus(`Floor: ${model.currentFloor.width}x${model.currentFloor.height} | Bodies: ${model.currentFloor.bodies.count}`);

      // Handle resize
      const onResize = () => {
        renderer.resize();
        renderer.syncToModel();
      };
      window.addEventListener('resize', onResize);

      return () => {
        window.removeEventListener('resize', onResize);
      };
    }

    const cleanup = init();

    return () => {
      cleanup.then((fn) => fn?.());
      if (app) {
        app.destroy(true);
      }
    };
  }, []);

  return (
    <div style={{ width: '100vw', height: '100vh', position: 'relative' }}>
      <div ref={canvasRef} style={{ width: '100%', height: '100%' }} />
      <div style={{
        position: 'absolute',
        top: 8,
        left: 8,
        color: '#aaa',
        fontFamily: 'monospace',
        fontSize: 12,
      }}>
        {status}
      </div>
    </div>
  );
}

export default App;
