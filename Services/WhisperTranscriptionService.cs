using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;
using Newtonsoft.Json;
using RecordWhisperClient.Models;
using RecordWhisperClient.Services;

namespace RecordWhisperClient.Services
{
    public class WhisperTranscriptionService : IDisposable
    {
        private readonly HttpClient httpClient;
        private readonly string whisperServerUrl;
        private readonly string transcriptionLanguage;
        private readonly string apiKey;

        public WhisperTranscriptionService(string serverUrl, string language = "auto", string apiKey = "")
        {
            whisperServerUrl = serverUrl;
            transcriptionLanguage = language;
            this.apiKey = apiKey;
            httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(5);
            
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                Logger.Info("Whisper client configured with API key authentication");
            }
            
            Logger.Info($"Whisper client configured with {httpClient.Timeout} timeout");
        }

        public async Task<string> TranscribeAudio(string audioFilePath)
        {
            Logger.Info($"Starting transcription for: {audioFilePath}");
            Logger.Info($"Whisper server URL: {whisperServerUrl}");
            Logger.Info($"Transcription language: {transcriptionLanguage}");

            try
            {
                using (var form = new MultipartFormDataContent())
                {
                    // Convert audio to mono if needed before sending
                    var processedAudioBytes = await ConvertToMonoIfNeeded(audioFilePath);
                    Logger.Info($"Processed audio file size: {processedAudioBytes.Length} bytes");
                    var fileContent = new ByteArrayContent(processedAudioBytes);
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");
                    form.Add(fileContent, "file", Path.GetFileName(audioFilePath));

                    form.Add(new StringContent("transcribe"), "task");
                    form.Add(new StringContent(transcriptionLanguage), "language");
                    form.Add(new StringContent("json"), "output");
                    Logger.Info($"Whisper parameters - task: transcribe, language: {transcriptionLanguage}, output: json");

                    string endpoint = whisperServerUrl.TrimEnd('/') + "/inference";
                    Logger.Info($"Sending request to: {endpoint}");

                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    var response = await httpClient.PostAsync(endpoint, form);
                    stopwatch.Stop();

                    Logger.Info($"Whisper server response: {response.StatusCode} (took {stopwatch.ElapsedMilliseconds}ms)");

                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        Logger.Info($"Received JSON response length: {jsonResponse.Length} characters");

                        var transcriptionResult = JsonConvert.DeserializeObject<WhisperResponse>(jsonResponse);

                        if (transcriptionResult != null && !string.IsNullOrWhiteSpace(transcriptionResult.Text))
                        {
                            Logger.Info($"Transcription successful. Text length: {transcriptionResult.Text.Length} characters");
                            return transcriptionResult.Text;
                        }
                        else
                        {
                            Logger.Warning("No speech detected in recording or empty response");
                            return null;
                        }
                    }
                    else
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        Logger.Error($"Whisper server error: {response.StatusCode} - {responseContent}");
                        throw new Exception($"Server returned: {response.StatusCode}");
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                Logger.Error($"HTTP connection error to Whisper server: {whisperServerUrl}", ex);
                throw new Exception("Could not connect to Whisper server", ex);
            }
            catch (TaskCanceledException ex)
            {
                Logger.Error("Transcription request timed out", ex);
                throw new Exception("Request timed out", ex);
            }
            catch (Exception ex)
            {
                Logger.Error("Transcription failed with unexpected error", ex);
                throw;
            }
        }

        public async Task SaveTranscription(string audioFilePath, string transcription, AudioRecordingService audioService)
        {
            try
            {
                string textFilePath = Path.Combine(Path.GetDirectoryName(audioFilePath), "transcription.txt");
                Logger.Info($"Saving transcription to: {textFilePath}");

                string content = $"Transcription generated on: {DateTime.Now}\n" +
                               $"Audio file: {Path.GetFileName(audioFilePath)}\n" +
                               $"Duration: {audioService.GetAudioDuration(audioFilePath)}\n\n" +
                               $"Transcription:\n{transcription}";

                await File.WriteAllTextAsync(textFilePath, content, Encoding.UTF8);
                Logger.Info($"Transcription saved successfully. File size: {content.Length} characters");
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to save transcription", ex);
                throw;
            }
        }

        private async Task<byte[]> ConvertToMonoIfNeeded(string audioFilePath)
        {
            try
            {
                using (var audioReader = new AudioFileReader(audioFilePath))
                {
                    Logger.Info($"Original audio format: {audioReader.WaveFormat.SampleRate}Hz, {audioReader.WaveFormat.Channels} channel(s), {audioReader.WaveFormat.BitsPerSample}-bit");
                    
                    // If already mono, just return the original file bytes
                    if (audioReader.WaveFormat.Channels == 1)
                    {
                        Logger.Info("Audio is already mono, using original file");
                        return await File.ReadAllBytesAsync(audioFilePath);
                    }
                    
                    // Convert to mono
                    Logger.Info("Converting stereo/multi-channel audio to mono");
                    
                    // Create mono format (44.1kHz, 16-bit, mono)
                    var monoFormat = new WaveFormat(44100, 16, 1);
                    
                    using (var memoryStream = new MemoryStream())
                    {
                        try
                        {
                            // Try MediaFoundation resampler first (higher quality)
                            using (var resampler = new MediaFoundationResampler(audioReader, monoFormat))
                            {
                                resampler.ResamplerQuality = 60; // High quality
                                
                                using (var waveFileWriter = new WaveFileWriter(memoryStream, monoFormat))
                                {
                                    var buffer = new byte[4096];
                                    int bytesRead;
                                    
                                    while ((bytesRead = resampler.Read(buffer, 0, buffer.Length)) > 0)
                                    {
                                        waveFileWriter.Write(buffer, 0, bytesRead);
                                    }
                                    
                                    waveFileWriter.Flush();
                                }
                            }
                        }
                        catch (Exception)
                        {
                            // If MediaFoundation fails, fall back to NAudio stereo-to-mono conversion
                            Logger.Info("MediaFoundation resampler not available, using NAudio StereoToMonoProvider16");
                            memoryStream.SetLength(0);
                            memoryStream.Position = 0;
                            
                            audioReader.Position = 0;
                            
                            // Convert to 16-bit first if needed, then to mono
                            var waveProvider = audioReader.ToWaveProvider16();
                            var monoProvider = new StereoToMonoProvider16(waveProvider);
                            
                            using (var waveFileWriter = new WaveFileWriter(memoryStream, monoProvider.WaveFormat))
                            {
                                var buffer = new byte[4096];
                                int bytesRead;
                                
                                while ((bytesRead = monoProvider.Read(buffer, 0, buffer.Length)) > 0)
                                {
                                    waveFileWriter.Write(buffer, 0, bytesRead);
                                }
                                
                                waveFileWriter.Flush();
                            }
                        }
                        
                        var convertedBytes = memoryStream.ToArray();
                        Logger.Info($"Audio converted to mono. New size: {convertedBytes.Length} bytes");
                        return convertedBytes;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to convert audio to mono, using original file: {ex.Message}");
                // If conversion fails, fall back to original file
                return await File.ReadAllBytesAsync(audioFilePath);
            }
        }

        public async Task<bool> TestConnection()
        {
            Logger.Info($"Testing connection to Whisper server: {whisperServerUrl}");
            
            try
            {
                string endpoint = whisperServerUrl.TrimEnd('/');
                
                // Try to reach the server with a simple GET request
                using (var testClient = new HttpClient())
                {
                    testClient.Timeout = TimeSpan.FromSeconds(10); // Shorter timeout for connection test
                    
                    if (!string.IsNullOrWhiteSpace(apiKey))
                    {
                        testClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                    }
                    
                    // Try different endpoints that Whisper servers commonly respond to
                    string[] testEndpoints = { "/", "/health", "/v1/health", "/inference" };
                    
                    foreach (string testPath in testEndpoints)
                    {
                        try
                        {
                            Logger.Info($"Testing endpoint: {endpoint}{testPath}");
                            var response = await testClient.GetAsync($"{endpoint}{testPath}");
                            
                            // Accept any response (200, 404, 405, etc.) as long as we get a response
                            Logger.Info($"Connection test response: {response.StatusCode} from {endpoint}{testPath}");
                            
                            if (response.StatusCode != System.Net.HttpStatusCode.RequestTimeout)
                            {
                                Logger.Info("Whisper server connection test successful");
                                return true;
                            }
                        }
                        catch (HttpRequestException ex)
                        {
                            Logger.Info($"Endpoint {testPath} not reachable: {ex.Message}");
                            continue; // Try next endpoint
                        }
                        catch (TaskCanceledException)
                        {
                            Logger.Info($"Endpoint {testPath} timed out");
                            continue; // Try next endpoint
                        }
                    }
                    
                    Logger.Warning("No responsive endpoints found on Whisper server");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Connection test failed: {ex.Message}");
                return false;
            }
        }

        public void Dispose()
        {
            httpClient?.Dispose();
        }
    }
}