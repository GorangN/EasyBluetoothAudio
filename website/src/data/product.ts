export const product = {
  name: 'EasyBluetoothAudio',
  repository: 'https://github.com/GorangN/EasyBluetoothAudio',
  download: 'https://github.com/GorangN/EasyBluetoothAudio/releases/latest/download/EasyBluetoothAudioSetup.exe',
  releases: 'https://github.com/GorangN/EasyBluetoothAudio/releases/latest',
  canonical: 'https://gorangn.github.io/EasyBluetoothAudio/',
};

export const questions = [
  {
    question: 'Free? What’s the catch?',
    answer: 'There’s no account, subscription, or trial period. EasyBluetoothAudio is a free, open-source project under the MIT license. You can use it, inspect the code, or build it yourself.',
  },
  {
    question: 'Will it work on my PC?',
    answer: 'You need an x64 PC running Windows 10 version 2004 (build 19041) or later, or Windows 11, with a working Bluetooth adapter. Pair your phone with Windows first. Compatibility depends on your phone and Bluetooth hardware; the app is designed for media playback, not phone calls or microphone routing.',
  },
  {
    question: 'Do I need an app on my phone or a separate .NET install?',
    answer: 'No companion phone app is needed. Keep using your usual media apps and pair your phone through Windows Bluetooth settings. The Windows installer includes the .NET runtime, and the receiver uses the native Windows audio system without extra audio drivers.',
  },
  {
    question: 'Where does my audio go?',
    answer: 'Your audio travels over Bluetooth between your phone and PC. App preferences are stored locally on your PC. The app contacts GitHub to check for updates. This website has no analytics scripts or signup form.',
  },
  {
    question: 'What if Windows shows a SmartScreen warning?',
    answer: 'SmartScreen checks the reputation of downloaded apps, and may warn about this installer. Before deciding to run it, confirm that it came from this project’s official GitHub Releases page. The source is also available if you prefer to review it or build the app yourself.',
  },
  {
    question: 'What if I’m connected but can’t hear anything?',
    answer: 'Check the volume on your phone and PC, then try Reconnect in the app. If the Bluetooth link itself has dropped, restore it and use Connect. You can report persistent issues on GitHub with your Windows version and Bluetooth hardware details.',
  },
];
