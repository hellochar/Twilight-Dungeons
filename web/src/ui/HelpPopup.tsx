import { useState, useEffect } from 'react';
import { FONT_FAMILY_SERIF, FontSize } from './fonts';
import { buttonBase } from './theme';
import { CloseButton } from './CloseButton';
import { MUTE_ICON_SIZE } from '../constants';

const SEEN_KEY = 'twilight-dungeons-help-seen';

const HELP_BUTTON_STYLE: React.CSSProperties = {
  ...buttonBase,
  padding: '6px 6px 0 6px',
  background: 'rgba(20, 20, 30, 0.85)',
  border: 'none',
  borderRadius: 0,
  color: '#ccc',
  fontSize: FontSize.xl,
  lineHeight: 1,
};

const ICON_STYLE: React.CSSProperties = { width: MUTE_ICON_SIZE, height: MUTE_ICON_SIZE, fill: 'currentColor' };

export function HelpButton() {
  const [open, setOpen] = useState(() => !localStorage.getItem(SEEN_KEY));

  function handleClose() {
    localStorage.setItem(SEEN_KEY, '1');
    setOpen(false);
  }

  return (
    <>
      <div style={{ position: 'absolute', top: 6, left: 10, zIndex: 20, pointerEvents: 'auto' }}>
        <button
          onClick={() => setOpen(true)}
          style={HELP_BUTTON_STYLE}
          title="Help"
        >
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 320 512" style={ICON_STYLE}>
            <path d="M80 160c0-35.3 28.7-64 64-64h32c35.3 0 64 28.7 64 64v3.6c0 21.8-11.1 42.1-29.4 53.8l-42.2 27.1c-5.2 3.3-8.4 9-8.4 15.1v15.4c0 13.3 10.7 24 24 24s24-10.7 24-24v-3.3l42.2-27.1C285 221.5 288 186.2 288 163.6V160c0-61.9-50.1-112-112-112H144C82.1 48 32 98.1 32 160c0 13.3 10.7 24 24 24s24-10.7 24-24zm80 288a32 32 0 1 0 0-64 32 32 0 1 0 0 64z"/>
          </svg>
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
          borderRadius: 4,
          padding: '24px',
          maxWidth: 600,
          width: '90vw',
          maxHeight: '80dvh',
          overflowY: 'auto',
          fontFamily: FONT_FAMILY_SERIF,
          color: '#fff',
          lineHeight: 1.5,
        }}
      >
        <div style={{ position: 'relative', display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 12 }}>
          <span style={{ fontSize: FontSize.xl }}>Twilight Dungeons</span>
          <CloseButton
            onClick={onClose}
            style={{ position: 'absolute', top: 0, right: 0, zIndex: 1 }}
          />
        </div>

        <Section title="">
          Defeat all enemies!
          <br/>
          Tap to move or attack.
          <br/>
          Clear the daily challenge on all three difficulties!
          <br/>
        </Section>

        <div style={{ borderTop: '1px solid #334', paddingTop: 10, marginTop: 10, fontFamily: FONT_FAMILY_SERIF, fontSize: FontSize.serifSm, color: '#888' }}>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 2, lineHeight: 1.5 }}>
            <span>Made for 7drl 2026 by <Link href="https://github.com/hellochar">hellochar</Link>.</span>
            <span>No AI was used in the art or audio assets. AI has contributed (significant) code.</span>
            <span>Play the original mobile app game on <Link href="https://play.google.com/store/apps/details?id=com.hellochar.TwilightEcologist">Google Play.</Link></span>
          </div>
        </div>
      </div>
    </div>
  );
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div style={{ marginBottom: 10 }}>
      <div style={{ fontSize: FontSize.lg, lineHeight: 1.6, color: '#aac', marginBottom: 4 }}>{title}</div>
      <ul style={{ margin: 0, paddingLeft: 0, fontSize: FontSize.md, lineHeight: 1.6 }}>
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
