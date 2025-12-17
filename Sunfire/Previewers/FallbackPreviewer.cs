using System.Diagnostics;
using Sunfire.FSUtils.Models;
using Sunfire.Tui.Enums;
using Sunfire.Tui.Interfaces;
using Sunfire.Tui.Models;
using Sunfire.Views;

namespace Sunfire.Previewers;

public class FallbackPreviewer : PreviewView.IPreviewer
{
    BashPreviewView previewView = new();

    public Task<IRelativeSunfireView?> Update(FSEntry entry, CancellationToken token)
    {
        previewView.TargetEntry = entry;

        return Task.FromResult<IRelativeSunfireView?>(previewView);
    }

    private class BashPreviewView : ReservedSpaceView
    {
        private static readonly string baseDir;
        private static readonly string cleanerPath;
        private static readonly string previewerPath;

        private static readonly Process cleaner;
        private static Process? previewer = null;

        public FSEntry? TargetEntry = null;


        static BashPreviewView()
        {
            baseDir = AppDomain.CurrentDomain.BaseDirectory;
            cleanerPath = Path.Combine(baseDir, "sunfire-kitty-cleaner");
            previewerPath = Path.Combine(baseDir, "sunfire-kitty-previewer");

            cleaner = new() 
            {
                StartInfo = new()
                {
                    FileName = cleanerPath,
                    UseShellExecute = false,
                    RedirectStandardOutput = false,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                }
            };
        }

        protected override Task OnArrange()
        {
            Program.Renderer.Clear(OriginX, OriginY, X, Y);

            return Task.CompletedTask;
        }

        protected override Task OnDraw()
        {
            EnsurePreviewerIsDead();

            if(TargetEntry is null)
                return Task.CompletedTask;

            string previewArgs = $"\"{TargetEntry.Value.Path}\" {SizeX} {SizeY} {OriginX} {OriginY}";

            previewer = new()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = previewerPath,
                    Arguments = previewArgs,
                    UseShellExecute = false,
                    RedirectStandardOutput = false,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                }
            };

            Program.Renderer.PostRender(() => 
            {
                previewer.Start(); 
                return Task.CompletedTask; 
            });

            return Task.CompletedTask;
        }

        protected override Task OnInvalidate()
        {
            EnsurePreviewerIsDead();

            cleaner.Start();

            return Task.CompletedTask;
        }

        private static void EnsurePreviewerIsDead()
        {
            if(previewer is not null && !previewer.HasExited)
            {
                var oldPreviewer = previewer;
                previewer = null;

                _ = Task.Run(() =>
                {
                    oldPreviewer.Kill(true);
                    oldPreviewer.Dispose();
                });
            }
        }

    }

    private abstract class ReservedSpaceView : IRelativeSunfireView
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }

        public FillStyle FillStyleX { get; private set; } = FillStyle.Max;
        public FillStyle FillStyleY { get; private set; } = FillStyle.Max;

        public int StaticX { get; private set; } = 0;
        public int StaticY { get; private set; } = 0;
        public float PercentX { get; private set; } = 0;
        public float PercentY { get; private set; } = 0;

        public int MinX { get; set; } = 0;
        public int MinY { get; set; } = 0;

        public int OriginX { get; set; }
        public int OriginY { get; set; }
        public int SizeX { get; set; }
        public int SizeY { get; set; }

        private bool Dirty;

        public async Task<bool> Arrange()
        {
            if(Dirty)
            {
                Dirty = false;

                await OnArrange();

                return true;
            }

            return false;
        }

        protected abstract Task OnArrange();

        public async Task Draw(SVContext context)
        {
            await OnDraw();
        }

        protected abstract Task OnDraw();

        public async Task Invalidate()
        {
            Dirty = true;

            await OnInvalidate();
        }

        protected abstract Task OnInvalidate();
    }
}
