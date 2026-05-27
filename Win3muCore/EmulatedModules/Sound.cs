/*
Win3mu - Windows 3 Emulator
Copyright (C) 2017 Topten Software.

Win3mu is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

Win3mu is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Win3mu.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Sharp86;

namespace Win3muCore
{
    [Module("SOUND", @"C:\WINDOWS\SYSTEM\SOUND.DLL")]
    public class Sound : Module32
    {
        // Win3.x SOUND.DLL provided a simple voice-based sound API.
        // These are stubs returning appropriate error/success values since the
        // voice-queue synthesizer has no modern equivalent.

        const int S_SERDVNA = -1;    // Device not available
        const int S_SERQFUL = -4;    // Queue full
        const int S_THRESHOLD = 1;   // WaitSoundState: threshold reached

        // Ordinal 1 - OpenSound
        [EntryPoint(0x0001)]
        public short OpenSound()
        {
            Log.WriteLine("Sound.OpenSound");
            // Return number of available voices (1 = success with 1 voice)
            return 1;
        }

        // Ordinal 2 - CloseSound
        [EntryPoint(0x0002)]
        public void CloseSound()
        {
            Log.WriteLine("Sound.CloseSound");
        }

        // Ordinal 3 - SetVoiceQueueSize
        [EntryPoint(0x0003)]
        public short SetVoiceQueueSize(short nVoice, short cbQueue)
        {
            Log.WriteLine("Sound.SetVoiceQueueSize: voice={0}, size={1}", nVoice, cbQueue);
            return 0; // success
        }

        // Ordinal 4 - SetVoiceNote
        [EntryPoint(0x0004)]
        public short SetVoiceNote(short nVoice, short nValue, short nLength, short nCdots)
        {
            Log.WriteLine("Sound.SetVoiceNote: voice={0}, note={1}, len={2}, dots={3}", nVoice, nValue, nLength, nCdots);
            return 0; // success
        }

        // Ordinal 5 - SetVoiceAccent
        [EntryPoint(0x0005)]
        public short SetVoiceAccent(short nVoice, short nTempo, short nVolume, short nMode, short nPitch)
        {
            Log.WriteLine("Sound.SetVoiceAccent: voice={0}, tempo={1}, vol={2}, mode={3}, pitch={4}", nVoice, nTempo, nVolume, nMode, nPitch);
            return 0; // success
        }

        // Ordinal 6 - SetVoiceEnvelope
        [EntryPoint(0x0006)]
        public short SetVoiceEnvelope(short nVoice, short nShape, short nRepeat)
        {
            Log.WriteLine("Sound.SetVoiceEnvelope: voice={0}, shape={1}, repeat={2}", nVoice, nShape, nRepeat);
            return 0; // success
        }

        // Ordinal 7 - SetSoundNoise
        [EntryPoint(0x0007)]
        public short SetSoundNoise(short nSource, short nDuration)
        {
            Log.WriteLine("Sound.SetSoundNoise: source={0}, duration={1}", nSource, nDuration);
            return 0; // success
        }

        // Ordinal 8 - SetVoiceSound
        [EntryPoint(0x0008)]
        public short SetVoiceSound(short nVoice, uint lFrequency, short nDuration)
        {
            Log.WriteLine("Sound.SetVoiceSound: voice={0}, freq={1}, duration={2}", nVoice, lFrequency, nDuration);
            return 0; // success
        }

        // Ordinal 9 - StartSound
        [EntryPoint(0x0009)]
        public short StartSound()
        {
            Log.WriteLine("Sound.StartSound");
            return 0; // success
        }

        // Ordinal 10 - StopSound
        [EntryPoint(0x000A)]
        public short StopSound()
        {
            Log.WriteLine("Sound.StopSound");
            return 0; // success
        }

        // Ordinal 11 - WaitSoundState
        [EntryPoint(0x000B)]
        public short WaitSoundState(short nState)
        {
            Log.WriteLine("Sound.WaitSoundState: state={0}", nState);
            // Return 0 = state reached immediately (no sound playing)
            return 0;
        }

        // Ordinal 12 - SyncAllVoices
        [EntryPoint(0x000C)]
        public short SyncAllVoices()
        {
            Log.WriteLine("Sound.SyncAllVoices");
            return 0; // success
        }

        // Ordinal 13 - CountVoiceNotes
        [EntryPoint(0x000D)]
        public short CountVoiceNotes(short nVoice)
        {
            // Return 0 notes in queue
            return 0;
        }

        // Ordinal 14 - GetThresholdEvent
        [EntryPoint(0x000E)]
        public uint GetThresholdEvent()
        {
            // Return NULL event handle (no threshold event)
            return 0;
        }

        // Ordinal 15 - GetThresholdStatus
        [EntryPoint(0x000F)]
        public short GetThresholdStatus()
        {
            // Return 0 = no voices have reached threshold
            return 0;
        }

        // Ordinal 16 - SetVoiceThreshold
        [EntryPoint(0x0010)]
        public short SetVoiceThreshold(short nVoice, short nNotes)
        {
            Log.WriteLine("Sound.SetVoiceThreshold: voice={0}, notes={1}", nVoice, nNotes);
            return 0; // success
        }
    }
}
