namespace Sharp6502
{
    /// <summary>
    /// A memory read hook.
    /// </summary>
    public class MemoryReadHook
    {
        /// <summary>
        /// The start address of the hook.
        /// </summary>
        public ushort startAddress;

        /// <summary>
        /// The end address of the hook.
        /// </summary>
        public ushort endAddress;

        /// <summary>
        /// The read function.
        /// </summary>
        public Func<ushort, byte> readFunc;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryReadHook"/> class.
        /// </summary>
        /// <param name="startAddress">The start address.</param>
        /// <param name="endAddress">The end address.</param>
        /// <param name="readFunc">The read function.</param>
        public MemoryReadHook(ushort startAddress, ushort endAddress, Func<ushort, byte> readFunc)
        {
            this.startAddress = startAddress;
            this.endAddress = endAddress;
            this.readFunc = readFunc;
        }
    }

    /// <summary>
    /// A memory write hook.
    /// </summary>
    public class MemoryWriteHook
    {
        /// <summary>
        /// The start address of the hook.
        /// </summary>
        public ushort startAddress;

        /// <summary>
        /// The end address of the hook.
        /// </summary>
        public ushort endAddress;

        /// <summary>
        /// The write function.
        /// </summary>
        public Action<ushort, byte> writeFunc;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryWriteHook"/> class.
        /// </summary>
        /// <param name="startAddress">The start address.</param>
        /// <param name="endAddress">The end address.</param>
        /// <param name="writeFunc">The write function.</param>
        public MemoryWriteHook(ushort startAddress, ushort endAddress, Action<ushort, byte> writeFunc)
        {
            this.startAddress = startAddress;
            this.endAddress = endAddress;
            this.writeFunc = writeFunc;
        }
    }

    /// <summary>
    /// A representation of the 6502's memory space.
    /// </summary>
    /// <remarks>
    /// Memory is implemented as a flat 64K memory space. The first 512 bytes
    /// is the zero page, of which the stack occupies the last 256 bytes 
    /// (0x0100 - 0x01FF). Memory locations can be hooked by other components
    /// to provide additional functionality.
    /// </remarks>
    public static class Memory
    {
        /// <summary>
        /// The memory lock.
        /// </summary>
        public static object memLock = new();

        /// <summary>
        /// The data.
        /// </summary>
        /// <remarks>
        /// The memory space is a flat 64K space, stored on the heap of the host.
        /// </remarks>
        public static byte[] data = new byte[0x10000];

        /// <summary>
        /// The list of registered memory read hooks
        /// </summary>
        public static List<MemoryReadHook> readHooks = new();

        /// <summary>
        /// The list of registered memory write hooks
        /// </summary>
        public static List<MemoryWriteHook> writeHooks = new();

        /// <summary>
        /// Reads a byte from the specified address.
        /// </summary>
        public static byte Read(ushort address, bool hideDebug = false)
        {
            // Check for read hooks
            foreach (var hook in readHooks)
            {
                if (address >= hook.startAddress && address <= hook.endAddress)
                {
                    // This address is hooked, so call the hook function instead
                    // of reading from main memory
                    return hook.readFunc(address);
                }
            }

            // No hooks, so we read from main memory
            return data[address];
        }

        /// <summary>
        /// Writes a byte to the specified address.
        /// </summary>
        public static void Write(ushort address, byte value, bool hideDebug = false)
        {
            // Write the value to memory before calling any hooks,
            // so debugging tools can see the value in memory.
            data[address] = value;

            // Check for write hooks
            foreach (var hook in writeHooks)
            {
                if (address >= hook.startAddress && address <= hook.endAddress)
                {
                    // Call the hook
                    hook.writeFunc(address, value);
                    return;
                }
            }
        }

        /// <summary>
        /// Registers a memory read hook.
        /// </summary>
        /// <param name="startAddress">The start address.</param>
        /// <param name="endAddress">The end address.</param>
        /// <param name="readFunc">The read function to be called.</param>
        /// <remarks>
        /// Allows a component to hook a memory address and provide additional
        /// functionality. For example, a keyboard component could hook memory
        /// address 0x4016 to provide access to the keyboard.
        /// </remarks>
        public static void RegisterReadHook(ushort startAddress, ushort endAddress, Func<ushort, byte> readFunc)
        {
            readHooks.Add(new MemoryReadHook(startAddress, endAddress, readFunc));
        }

        /// <summary>
        /// Registers a memory write hook.
        /// </summary>
        /// <param name="startAddress">The start address.</param>
        /// <param name="endAddress">The end address.</param>
        /// <param name="writeFunc">The write function to be called.</param>
        /// <remarks>
        /// Allows a component to hook a memory address and provide additional
        /// functionality. For example, the audio component could hook memory
        /// addresses 0x4000 through 0x4013 to provide access to the audio registers.
        /// </remarks>
        public static void RegisterWriteHook(ushort startAddress, ushort endAddress, Action<ushort, byte> writeFunc)
        {
            writeHooks.Add(new MemoryWriteHook(startAddress, endAddress, writeFunc));
        }
    }
}