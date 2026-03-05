import { useState, useEffect } from 'react';
import { FONT_FAMILY_SERIF, FontSize } from './fonts';
import { buttonBase, buttonPaddingCompact } from './theme';

const SEEN_KEY = 'twilight-dungeons-help-seen';

export function HelpButton() {
  const [open, setOpen] = useState(() => !localStorage.getItem(SEEN_KEY));

  function handleClose() {
    localStorage.setItem(SEEN_KEY, '1');
    setOpen(false);
  }

  return (
    <>
      <div style={{ position: 'absolute', top: 8, left: 8, zIndex: 20, pointerEvents: 'auto' }}>
        <button
          onClick={() => setOpen(true)}
          style={{
            ...buttonBase,
            ...buttonPaddingCompact,
            background: 'rgba(16, 16, 24, 0.88)',
            color: '#aaa',
            border: '1px solid #445',
          }}
        >
          ?
        </button>
      </div>
      {open && <HelpPopup onClose={handleClose} />}
    </>
  );
}

function HelpPopup({ onClose }: { onClose: () => void }) {
  useEffect(() => {
    const handleKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
    };
    window.addEventListener('keydown', handleKey);
    return () => window.removeEventListener('keydown', handleKey);
  }, [onClose]);

  return (
    <div
      onClick={onClose}
      style={{
        position: 'fixed',
        inset: 0,
        background: 'rgba(0, 0, 0, 0.6)',
        zIndex: 200,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        pointerEvents: 'auto',
      }}
    >
      <div
        onClick={e => e.stopPropagation()}
        style={{
          background: 'rgba(16, 16, 24, 0.95)',
          border: '1px solid #88f',
          borderRadius: 8,
          padding: '16px 24px',
          maxWidth: 600,
          width: '90vw',
          maxHeight: '80dvh',
          overflowY: 'auto',
          fontFamily: FONT_FAMILY_SERIF,
          color: '#ccc',
        }}
      >
        <div style={{ position: 'relative', display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 12 }}>
          <span style={{ fontSize: FontSize.xl }}>Twilight Dungeons</span>
          <button
            onClick={onClose}
          style={{
            ...buttonBase,
            ...buttonPaddingCompact,
            position: 'absolute',
            top: 0,
            right: 0,
            background: 'transparent',
            border: 'none',
            color: '#888',
            lineHeight: 1,
            zIndex: 1,
          }}
        >
          ✕
        </button>
        </div>

        <div style={{ marginBottom: 20 }}>
          Made by {' '}
          <Link href="https://github.com/hellochar">hellochar</Link>
        </div>

        <Section title="">
          <li>Tap to inspect a creature, grass, move, or attack.</li>
          <li>Clear the floor in as few turns as possible.</li>
          <li>Clear the daily challenge on all three difficulties!</li>
        </Section>

        {/* <div style={{ borderTop: '1px solid #334', paddingTop: 10, marginTop: 10, fontSize: FontSize.sm, color: '#888' }}>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
            <span>Art and sound are a mixture of self made and CC0, MIT, and CC BY-NC licenses.</span>
            <span>Sound effects: <Link href="https://www.fesliyanstudios.com">Fesliyan Studios</Link></span>
            <span><Link href="https://www.dafont.com/coders-crux.font">Coder's Crux</Link> by NAL</span>
            <span>Font: <Link href="https://fonts.google.com/specimen/Libre+Baskerville">Libre Baskerville</Link> by Impallari Type (SIL OFL)</span>
          </div>
        </div> */}
      </div>
    </div>
  );
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div style={{ marginBottom: 10 }}>
      <div style={{ fontSize: FontSize.lg, color: '#aac', marginBottom: 4 }}>{title}</div>
      <ul style={{ margin: 0, paddingLeft: 20, fontSize: FontSize.md, lineHeight: 1.6 }}>
        {children}
      </ul>
    </div>
  );
}

function Link({ href, children }: { href: string; children: React.ReactNode }) {
  return (
    <a
      href={href}
      target="_blank"
      rel="noopener noreferrer"
      style={{ color: '#88f', textDecoration: 'underline' }}
    >
      {children}
    </a>
  );
}
