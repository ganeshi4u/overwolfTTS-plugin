using System;
using System.Collections.Generic;
using System.Speech.Synthesis;
using CSCore;
using CSCore.MediaFoundation;
using CSCore.SoundOut;
using System.IO;
using System.Web;
using System.Web.Script.Serialization;
using System.Linq;
using System.Threading;
using WindowsInput.Native;
using WindowsInput;

namespace OverwolfPluginTTS
{
    public class GenerateSpeech
    {
        public GenerateSpeech() {
            }

        public static string configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "InGameNarrator");
        public static string configPathFile = Path.Combine(configPath, "MyInGameNarratorSettings.json");
        SpeechSynthesizer speechSynthesizerObj = new SpeechSynthesizer();

        // Narrator options defaults
        public static string narrator_toggle = "on";
        public static int speechRate = 2;
        public static int outputDevice = 0;
        public static int narratorVolume = 80;
        public static string speechVoice = "Microsoft David Desktop";
        public static string voiceKey = "";

        public void setNarratorSettings ()
        {
            if (File.Exists(configPathFile))
            {
                dynamic json_data = System.Web.Helpers.Json.Decode(File.ReadAllText(configPathFile));
                narrator_toggle = json_data.hidden_narrator_toggle;
                speechRate = Int32.Parse(json_data.speech_rate_slider);
                outputDevice = Int32.Parse(json_data.output_device);
                narratorVolume = Int32.Parse(json_data.narrator_volume_slider);
                speechVoice = json_data.narrator_voice;
                voiceKey = json_data.voice_key;
            }
        }

        public void getNarratorSettings (Action<object> callback)
        {
            setNarratorSettings();
            List<object> settings = new List<object>();
            settings.Add(narrator_toggle);
            settings.Add(speechVoice);
            settings.Add(outputDevice);
            settings.Add(speechRate);
            settings.Add(narratorVolume);
            settings.Add(voiceKey);
            callback(settings.ToArray());
        }

        public void updateNarratorSettings (string queryString, Action<object> callback)
        {
            var data = HttpUtility.ParseQueryString(queryString);
            var json_data = new JavaScriptSerializer().Serialize(
                                      data.AllKeys.ToDictionary(mykey => mykey, mykey => data[mykey]));

            try
            {
                if (!Directory.Exists(configPath))
                {
                    Directory.CreateDirectory(configPath);
                }
                File.WriteAllText(configPathFile, json_data.ToString());

                if (File.Exists(configPathFile))
                    callback(true);
                else
                    callback(false);
            }
            catch (IOException e)
            {
                callback(false);
                Console.WriteLine(e.Message);
            }
        }

        public void convertText(String message)
        {
            Thread thread = new Thread(() => convertTextHandler(message));
            thread.Start();
        }
        public void convertTextHandler (String message)
        {
            lock (this)
            {
                setNarratorSettings();

                if (message == null)
                {
                    return;
                }

                if (narrator_toggle == "off")
                {
                    return;
                }

                var ins = new InputSimulator();
                var memoryStream = new MemoryStream();
                speechSynthesizerObj.Rate = speechRate;
                speechSynthesizerObj.SelectVoice(speechVoice);
                speechSynthesizerObj.Volume = narratorVolume;
                speechSynthesizerObj.SetOutputToWaveStream(memoryStream);
                speechSynthesizerObj.Speak(message);

                using (var waveOut = new WaveOut { Device = new WaveOutDevice(outputDevice) })
                using (var waveSource = new MediaFoundationDecoder(memoryStream))
                {
                    waveOut.Initialize(waveSource);
                    ins.Keyboard.KeyDown(VirtualKeyCode.VK_K);
                    waveOut.Play();
                    waveOut.WaitForStopped();
                    ins.Keyboard.KeyUp(VirtualKeyCode.VK_K);
                }
            }
        }

        public void getNarratorVoices (Action<object, object> callback)
        {
            if (callback == null)
                return;

            List<string> avail_voices = new List<string>();
            foreach (InstalledVoice voice in speechSynthesizerObj.GetInstalledVoices())
            {
                VoiceInfo info = voice.VoiceInfo;
                avail_voices.Add(info.Name);
            }

            if (avail_voices != null)
                callback(true, avail_voices.ToArray());
            else
                callback(false, "");
        }

        public void getOutputDevices (Action<object, object, object> callback)
        {
            if (callback == null)
                return;

            List<string> avail_output_devices = new List<string>();
            List<int> avail_output_devices_id = new List<int>();

            foreach (var device in WaveOutDevice.EnumerateDevices())
            {
                avail_output_devices.Add(device.Name);
                avail_output_devices_id.Add(device.DeviceId);
            }

            if (avail_output_devices != null)
                callback(true, avail_output_devices.ToArray(), avail_output_devices_id.ToArray());
            else
                callback(false, "", "");
        }
    }
}
