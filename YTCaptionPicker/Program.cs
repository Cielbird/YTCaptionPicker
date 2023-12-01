using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Channels;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using YoutubeExplode.Videos.ClosedCaptions;
using System.Text.RegularExpressions;
using System.IO;
using System.Diagnostics;

namespace YTCaptionPicker
{
    class Program
    {
        static async Task Main(string[] args)
        {



            //string targetFolder = "\\Users\\guilhemane\\Documents\\BlenderProjects\\Resources\\GetInClips";

            YoutubeClient client = new YoutubeClient();

            await foreach(PlaylistVideo vid in client.Channels.GetUploadsAsync("UCW5OrUZ4SeUYkUg1XqcjFYA"))
            {
                if(Regex.IsMatch(vid.Title, "ission"))
                {
                    await DownloadMatchesFromVidAsync(client,
                        vid,
                        "get in",
                        "/Users/guilhemane/Documents/YTFinds");
                }
            }
            
        }

        private static async Task DownloadMatchesFromChannel(YoutubeClient client,
            string channelUrl, string match, string folderPath, int count)
        {
            VideoClient videos = client.Videos;

            int i = 0;


            await foreach (PlaylistVideo vid in client.Channels.GetUploadsAsync(channelUrl))
            {
                await DownloadMatchesFromVidAsync(client, vid, match, folderPath);
                i++;
                if (i == count)
                    return;
            }
        }

        public static async Task DownloadMatchesFromVidAsync(YoutubeClient client,
            IVideo vid, string match, string folderPath)
        {
            VideoClient videos = client.Videos;
            ClosedCaptionManifest trackManifest = await videos.ClosedCaptions.GetManifestAsync(vid.Id);

            if (trackManifest.Tracks.Count == 0)
                return;

            ClosedCaptionTrackInfo trackInfo = trackManifest.Tracks[0];// trackManifest.GetByLanguage("en");
            var track = await videos.ClosedCaptions.GetAsync(trackInfo);
            Console.WriteLine(vid.Title + "...");
            foreach (ClosedCaption cc in track.Captions)
            {
                if (Regex.IsMatch(cc.Text, match))
                {
                    await DownloadVidAsync(videos.Streams, vid.Id, folderPath, cc.Offset, cc.Duration);
                    Console.WriteLine(" - at " + cc.Offset);
                }
            }
        }

        public static async Task DownloadVidAsync(StreamClient streams,
            string id, string folderPath,
            TimeSpan fromTimespan, TimeSpan cutDuration)
        {
            StreamManifest streamManifest = await streams.GetManifestAsync(id);
            var streamInfo = streamManifest.GetMuxedStreams().GetWithHighestVideoQuality();

            string path = $"{folderPath}/{id}{fromTimespan}.mp4";
            FileStream fileStream = null;
            if (!File.Exists(path))
                fileStream = File.Create(path);
            else
                return;

            var ffmpeg = @"/usr/local/bin/ffmpeg";
            var arguments = $"-ss {fromTimespan} -i \"{streamInfo.Url}\" -y -t {cutDuration} -c:v libx264 -pix_fmt yuv420p \"{path}\"";

            using (Process p = new Process())
            {
                p.StartInfo.FileName = ffmpeg;
                p.StartInfo.Arguments = arguments;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.Start();
                p.WaitForExit();

                var output = p.StandardOutput.ReadToEnd();
            }
        }

    }
}
