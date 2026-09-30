using System;
using System.Numerics;
using System.Runtime.InteropServices;
using OpenTabletDriver.Plugin.Platform.Display;
using OpenTabletDriver.Plugin.Platform.Pointer;

namespace VoiDPlugins.OutputMode
{
    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT
    {
        public INPUT_TYPE type;
        public MOUSEINPUT mouse;
        public static int Size => Marshal.SizeOf(typeof(INPUT));
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public MOUSEEVENTF dwFlags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    public enum MOUSEEVENTF : uint
    {
        ABSOLUTE = 0x8000,
        MOVE = 0x0001,
        VIRTUALDESK = 0x4000,
        LEFTDOWN = 0x0002,
        LEFTUP = 0x0004,
        MIDDLEDOWN = 0x0020,
        MIDDLEUP = 0x0040,
        RIGHTDOWN = 0x0008,
        RIGHTUP = 0x0010,
        XDOWN = 0x0080,
        XUP = 0x0100,
        MOVE_NOCOALESCE = 0x2000
    }

    public enum INPUT_TYPE
    {
        MOUSE_INPUT,
        KEYBD_INPUT,
        HARDWARE_INPUT
    }

    public class ThinOSPointer
    {
        private readonly Vector2 _conversion;

        public ThinOSPointer(IVirtualScreen screen)
        {
            _conversion = new Vector2(screen.Width, screen.Height) / 65535;
        }

        public void SetPosition(Vector2 pos)
        {
            var converted = pos / _conversion;
            var input = CreateInput(
                MOUSEEVENTF.ABSOLUTE | MOUSEEVENTF.MOVE | MOUSEEVENTF.VIRTUALDESK,
                (int)converted.X,
                (int)converted.Y);
            _ = SendInput(1, new[] { input }, INPUT.Size);
        }

        public void SetPositionAndButton(Vector2 pos, MouseButton button, bool isDown)
        {
            var (buttonFlags, mouseData) = GetButtonInput(button, isDown);
            if (buttonFlags == 0)
                return;

            var converted = pos / _conversion;
            INPUT[] inputs =
            {
                CreateInput(
                    MOUSEEVENTF.ABSOLUTE | MOUSEEVENTF.MOVE | MOUSEEVENTF.VIRTUALDESK,
                    (int)converted.X,
                    (int)converted.Y),
                CreateInput(buttonFlags, mouseData: mouseData)
            };
            _ = SendInput((uint)inputs.Length, inputs, INPUT.Size);
        }

        public void SetMouseButton(MouseButton button, bool isDown)
        {
            var (buttonFlags, mouseData) = GetButtonInput(button, isDown);
            if (buttonFlags == 0)
                return;

            INPUT[] inputs = { CreateInput(buttonFlags, mouseData: mouseData) };
            _ = SendInput(1, inputs, INPUT.Size);
        }

        private static INPUT CreateInput(MOUSEEVENTF flags, int x = 0, int y = 0, uint mouseData = 0)
        {
            return new INPUT
            {
                type = INPUT_TYPE.MOUSE_INPUT,
                mouse = new MOUSEINPUT
                {
                    dx = x,
                    dy = y,
                    mouseData = mouseData,
                    dwFlags = flags,
                    time = 0,
                    dwExtraInfo = UIntPtr.Zero
                }
            };
        }

        private static (MOUSEEVENTF Flags, uint MouseData) GetButtonInput(MouseButton button, bool isDown)
        {
            return button switch
            {
                MouseButton.Left => (isDown ? MOUSEEVENTF.LEFTDOWN : MOUSEEVENTF.LEFTUP, 0),
                MouseButton.Middle => (isDown ? MOUSEEVENTF.MIDDLEDOWN : MOUSEEVENTF.MIDDLEUP, 0),
                MouseButton.Right => (isDown ? MOUSEEVENTF.RIGHTDOWN : MOUSEEVENTF.RIGHTUP, 0),
                MouseButton.Backward => (isDown ? MOUSEEVENTF.XDOWN : MOUSEEVENTF.XUP, 1),
                MouseButton.Forward => (isDown ? MOUSEEVENTF.XDOWN : MOUSEEVENTF.XUP, 2),
                _ => (0, 0)
            };
        }

        [DllImport("user32.dll")]
        private static extern uint SendInput(uint nInputs, [MarshalAs(UnmanagedType.LPArray), In] INPUT[] pInputs, int cbSize);
    }
}