using System.Diagnostics;
using Sunfire.FSUtils.Models;
using Sunfire.Tui.Enums;
using Sunfire.Tui.Interfaces;
using Sunfire.Tui.Models;
using Sunfire.Views;

namespace Sunfire.Previewers;

public class FallbackPreviewer : PreviewView.IPreviewer
{
    private BashPreviewView? view;

    public Task<IRelativeSunfireView?> Update(FSEntry entry)
    {
        view ??= new();
        view.UpdateEntry(entry);
        return Task.FromResult<IRelativeSunfireView?>(view);
    }

    public Task CleanUp()
    {
        view?.Dispose();
        view = null;

        return Task.CompletedTask;
    }

    private class BashPreviewView : IRelativeSunfireView, IDisposable
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }

        public FillStyle FillStyleX { get; private set; } = FillStyle.Max;
        public FillStyle FillStyleY { get; private set; } = FillStyle.Max;
        public int StaticX { get; private set; } = 1;
        public int StaticY { get; private set; } = 1;
        public float PercentX { get; private set; } = 1.0f;
        public float PercentY { get; private set; } = 1.0f;

        public int OriginX { get; set; }
        public int OriginY { get; set; }
        public int SizeX { get; set; }
        public int SizeY { get; set; }

        public int MinX { get; private set; } = 0;
        public int MinY { get; private set; } = 0;

        private FSEntry? _entry;

        private Process? process;
        private bool Dirty;

        private bool disposed = false;

        private (int, int, int, int)? lastLayout;

        public void UpdateEntry(FSEntry entry)
        {
            _entry = entry;
            Dirty = true;
        }

        public Task<bool> Arrange()
        {
            var layout = (OriginX, OriginY, SizeX, SizeY);
            bool layoutChanged = layout != lastLayout;
            lastLayout = layout;

            if(Dirty || layoutChanged)
            {
                Dirty = false;
                
                StopProcess();
                ClearRegion();

                Program.Renderer.PostRender(() =>
                {
                    if(!disposed)
                        StartProcess();
                    
                    return Task.CompletedTask;
                });

                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        public Task Draw(SVContext _) => Task.CompletedTask;

        public Task Invalidate() => Task.CompletedTask;

        public void Dispose()
        {
            disposed = true;
            StopProcess();
            ClearRegion();
            RunCleaner();
        }

        private void ClearRegion()
        {
            Program.Renderer.Clear(OriginX, OriginY, SizeX, SizeY);
        }

        private void StartProcess()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string previewerPath = Path.Combine(baseDir, "sunfire-kitty-previewer");

            process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = previewerPath,
                    Arguments = $"\"{_entry!.Value.Path}\" {SizeX} {SizeY} {OriginX} {OriginY}",
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
        }   

        private void StopProcess()
        {
            if (process is not null && !process.HasExited)
            {
                var oldProcess = process;
                process = null;
                _ = Task.Run(() =>
                {
                    oldProcess.Kill(true);
                    oldProcess.Dispose();
                });
            }
        }

        public static void RunCleaner()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string cleanerPath = Path.Combine(baseDir, "sunfire-kitty-cleaner");

            var cleaner = Process.Start(new ProcessStartInfo
            {
                FileName = cleanerPath,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            _ = Task.Run(() =>
            {
                cleaner?.WaitForExit();
            });
        }
    }
}
