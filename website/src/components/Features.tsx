import { Bluetooth, ChevronUp, Headphones, Volume2, Settings2, Check } from 'lucide-react';
import { BentoCard, BentoGrid } from './magicui/bento-grid';

export default function Features() {
  return (
    <BentoGrid className="feature-grid">
      <BentoCard className="feature-listening" name="Your phone stays in control." description="Keep choosing music, podcasts, and videos in the apps you already use. Hear them through the speakers or headphones connected to your PC." background={
        <div className="listening-art" aria-hidden="true"><Headphones size={112} strokeWidth={1.2} /><div className="listening-wave">{[20, 34, 58, 84, 46, 68, 100, 72, 44, 62, 86, 48, 24, 38, 18].map((height, index) => <span key={index} style={{height: `${height}%`}} />)}</div><span>Same playlist.<br />A different place to listen.</span></div>
      } />
      <BentoCard className="feature-tray" name="Out of your way. Within reach." description="Connection controls live in your Windows system tray. Enable Auto-Connect to reconnect to your selected phone when the app starts." background={
        <div className="tray-art" data-ui aria-label="Illustration of tray access and the Auto-Connect setting"><div className="setting-preview"><Settings2 size={17} /><span>Auto-Connect</span><span className="switch-on"><Check size={13} /></span></div><div className="tray-bar"><span>Windows system tray</span><ChevronUp size={16} /><Bluetooth size={19} /><Volume2 size={18} /></div></div>
      } />
    </BentoGrid>
  );
}
