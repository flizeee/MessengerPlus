using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public class KeyboardHook
{

    // System Code: Low Level Keyboard Hook
    private const int WH_KEYBOARD_LL = 13;

    // System Event Code: Key Down Event
    private const int WM_KEYDOWN = 0x100;

    // System Event Code: System Key Down Event
    private const int WM_SYSKEYDOWN = 0x104;

    // Stores the handle to the Keyboard hook procedure.
    private static int s_KeyboardHookHandle;

    //是否只由這個Global Hook抓取鍵盤事件
    public static bool globalControlOnly = false;

    //Private KeyDown Event, 與GlobalKeyDown配合使用
    private static event KeyEventHandler _globalKeyDown;

    // Public KeyDown Event
    // 每次只能一個Event Handler處理這個Event
    // 加入和解除EventHandler時會自動安裝和解除Hook
    public static event KeyEventHandler GlobalKeyDown
    {
        add
        {
            KeyboardHook.HookKeyboard();
            KeyboardHook._globalKeyDown += value;
        }
        remove
        {
            KeyboardHook._globalKeyDown -= value;
            KeyboardHook.UnhookKeyboard();
        }
    }




    // 當hook抓到key event時的處理程序
    public delegate int HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    // Hook handle
    private static int m_HookHandle = 0;

    // Keyboard Hook函式指標
    private static HookProc m_KbdHookProc;

    // WinAPI 取得Module Handle
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    // WinAPI 加入Hook
    [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
    public static extern int SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hInstance, int threadId);


    // WinAPI 解除Hook
    [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
    public static extern bool UnhookWindowsHookEx(int idHook);


    // WinAPI  將Event傳給下一個Hook，如不執行此項，則只有這個Hook會被執行
    [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
    public static extern int CallNextHookEx(int idHook, int nCode, IntPtr wParam, IntPtr lParam);


    private static void HookKeyboard()
    {
        if (m_HookHandle == 0)
        {

            KeyboardHook.s_KeyboardHookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, m_KbdHookProc,
                                                                 Marshal.GetHINSTANCE(Assembly.GetExecutingAssembly().GetModules()[0]), 0);

            using (Process curProcess = Process.GetCurrentProcess())
            {
                using (ProcessModule curModule = curProcess.MainModule)
                {
                    m_KbdHookProc = new HookProc(KeyboardHookProc);
                    m_HookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, m_KbdHookProc, GetModuleHandle(curModule.ModuleName), 0);
                }
            }

            if (m_HookHandle == 0)
            {
                throw new Exception("Install Global Keyboard Hook Faild.");
            }

        }
    }


    private static void UnhookKeyboard()
    {

        if (m_HookHandle != 0)
        {
            bool ret = UnhookWindowsHookEx(m_HookHandle);
            if (ret)
            {
                m_HookHandle = 0;
            }
            else
            {
                throw new Exception("Uninstall Global Keyboard Hook Faild.");
            }

        }

    }





    private static int KeyboardHookProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        bool handled = false;

        if (nCode >= 0)
        {

            if ((int)wParam == WM_KEYDOWN || (int)wParam == WM_SYSKEYDOWN)
            {
                KeyboardHookStruct MyKeyboardHookStruct = (KeyboardHookStruct)Marshal.PtrToStructure(lParam, typeof(KeyboardHookStruct));

                Keys keyData = (Keys)MyKeyboardHookStruct.VirtualKeyCode;
                KeyEventArgs e = new KeyEventArgs(keyData);

                if (KeyboardHook.globalControlOnly)
                {
                    e.Handled = true;
                }
                else
                {
                    e.Handled = false;
                }

                _globalKeyDown.Invoke(null, e);

                handled = e.Handled;
            }

        }


        if (KeyboardHook.globalControlOnly) return -1;

        return CallNextHookEx(s_KeyboardHookHandle, nCode, wParam, lParam);


    }



    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardHookStruct
    {
        /// 

        /// Specifies a virtual-key code. The code must be a value in the range 1 to 254. 
        /// 
        public int VirtualKeyCode;
        /// 

        /// Specifies a hardware scan code for the key. 
        /// 
        public int ScanCode;
        /// 

        /// Specifies the extended-key flag, event-injected flag, context code, and transition-state flag.
        /// 
        public int Flags;
        /// 

        /// Specifies the Time stamp for this message.
        /// 
        public int Time;
        /// 

        /// Specifies extra information associated with the message. 
        /// 
        public int ExtraInfo;
    }

}