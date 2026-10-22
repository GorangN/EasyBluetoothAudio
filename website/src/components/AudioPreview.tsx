import { useEffect, useRef, useState } from 'react';
import { Bluetooth, Headphones, Speaker, Smartphone, Music2, Pause, Play, Settings2, RefreshCw, ChevronDown, SkipBack, SkipForward, Volume2 } from 'lucide-react';
import { AnimatedBeam } from './magicui/animated-beam';

export default function AudioPreview() {
  const containerRef = useRef<HTMLDivElement>(null);
  const phoneRef = useRef<HTMLDivElement>(null);
  const appRef = useRef<HTMLDivElement>(null);
  const outputRef = useRef<HTMLDivElement>(null);
  const [output, setOutput] = useState<'speakers' | 'headphones'>('speakers');
  const [paused, setPaused] = useState(true);
  const [ready, setReady] = useState(false);

  useEffect(() => setReady(true), []);

  useEffect(() => {
    if (paused) {
      return;
    }
    const timeout = window.setTimeout(() => setPaused(true), 6000);
    return () => window.clearTimeout(timeout);
  }, [paused]);

  return (
    <div className="audio-preview" data-motion={paused ? 'paused' : 'playing'}>
      <div className="preview-toolbar">
        <span><Bluetooth size={16} aria-hidden="true" /> Your phone → your Windows PC</span>
        <button className="motion-toggle" disabled={!ready} onClick={() => setPaused(!paused)} aria-pressed={paused} aria-label={paused ? 'Play preview animation' : 'Pause preview animation'}>
          {paused ? <Play size={15} aria-hidden="true" /> : <Pause size={15} aria-hidden="true" />}
          <span>{paused ? 'Play preview animation' : 'Pause preview animation'}</span>
        </button>
      </div>
      <div className="audio-route" ref={containerRef}>
        <AnimatedBeam className="phone-beam" containerRef={containerRef} fromRef={phoneRef} toRef={appRef} pathColor="#d8ec44" pathOpacity={0.25} gradientStartColor="#d8ec44" gradientStopColor="#d8ec44" duration={3} repeat={0} paused={paused} />
        <AnimatedBeam containerRef={containerRef} fromRef={appRef} toRef={outputRef} pathColor="#d8ec44" pathOpacity={0.25} gradientStartColor="#d8ec44" gradientStopColor="#d8ec44" duration={3} delay={1.5} repeat={0} paused={paused} />
        <div className="phone-node" ref={phoneRef}>
          <div className="phone-art" aria-hidden="true">
            <span className="phone-notch" />
            <div className="album-art"><Music2 size={40} strokeWidth={1.5} /></div>
            <strong>Your favorite track</strong><span>Your music. Your apps.</span>
            <div className="music-progress"><span /></div>
            <div className="player-controls"><SkipBack size={17} /><Pause size={23} fill="currentColor" /><SkipForward size={17} /></div>
          </div>
          <p><Smartphone size={16} aria-hidden="true" /> Play on your phone</p>
        </div>
        <div className="app-node" ref={appRef}>
          <div className="app-window" data-ui>
            <div className="window-title"><span><Bluetooth size={18} /> EasyBluetoothAudio</span><span aria-hidden="true">− &nbsp; ×</span></div>
            <div className="window-content">
              <div className="app-heading"><strong>Audio sink</strong><span aria-hidden="true"><RefreshCw size={15} /><Settings2 size={15} /></span></div>
              <div className="device-select"><Smartphone size={20} /><span>My phone</span><ChevronDown size={16} /></div>
              <div className="connection-state"><span className="status-dot" /> Connected<span className="signal-bars" aria-hidden="true">{[8, 17, 12, 23, 16, 10, 20, 14].map((height, index) => <i key={index} style={{ height, animationDelay: `${index * -0.13}s` }} />)}</span></div>
              <div className="app-actions" aria-hidden="true"><span>Reconnect</span><span>Disconnect</span></div>
            </div>
          </div>
          <p>Interface preview · no device connection</p>
        </div>
        <div className="output-node" ref={outputRef}>
          <div className="output-art" aria-hidden="true">{output === 'speakers' ? <div className="speaker-pair"><div><i /><b /></div><div><i /><b /></div></div> : <Headphones className="headphone-art" size={128} strokeWidth={1.4} />}</div>
          <p><Volume2 size={16} aria-hidden="true" /> {output === 'speakers' ? 'Listen through PC speakers' : 'Listen through PC headphones'}</p>
          <div className="output-switch" role="group" aria-label="Preview your PC audio output">
            <button disabled={!ready} aria-pressed={output === 'speakers'} onClick={() => setOutput('speakers')}><Speaker size={14} aria-hidden="true" /> Speakers</button>
            <button disabled={!ready} aria-pressed={output === 'headphones'} onClick={() => setOutput('headphones')}><Headphones size={14} aria-hidden="true" /> Headphones</button>
          </div>
          <span className="sr-only" aria-live="polite">Preview output: {output}</span>
        </div>
      </div>
    </div>
  );
}
