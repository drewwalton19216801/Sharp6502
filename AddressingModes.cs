using System.Reflection;

namespace Sharp6502
{
    /// <summary>
    /// Addressing mode functions.
    /// </summary>
    public static class AddressingModes
    {
        /// <summary>
        /// Gets the address of an instruction.
        /// </summary>
        /// <param name="addressingMode">The addressing mode.</param>
        /// <returns>A byte.</returns>
        public static byte GetAddress(string addressingMode)
        {
            // --------------------------------------
            // We're going to use reflection to get the method that corresponds to the addressing mode,
            // and then invoke it. This is a lot faster than a switch statement, and it's a lot easier
            // to maintain.
            // --------------------------------------

            // Get the method name
            string methodName = addressingMode.ToString();

            // Get the method
            MethodInfo? method = typeof(AddressingModes).GetMethod(methodName) ?? throw new Exception("Invalid addressing mode: " + addressingMode);

            // Invoke the method
            object? result = (byte?)method.Invoke(null, new object[] { });

            // Handle the result
            if (result is byte byteResult)
            {
                return byteResult;
            }
            else
            {
                throw new Exception("Invalid addressing mode: " + addressingMode);
            }
        }

        /// <summary>
        /// The implied addressing mode.
        /// </summary>
        /// <returns>1 if an extra cycle was used, 0 otherwise</returns>
        public static byte Implied()
        {
            // Set the fetched byte to the value of the accumulator
            CPU.fetchedByte = Registers.A;

            // This mode never uses an extra cycle
            return 0;
        }

        /// <summary>
        /// The immediate addressing mode.
        /// </summary>
        /// <returns>1 if an extra cycle was used, 0 otherwise</returns>
        public static byte Immediate()
        {
            // Set the absolute address to the program counter
            CPU.addressAbsolute = Registers.PC++;

            // This mode never uses an extra cycle
            return 0;
        }

        /// <summary>
        /// The zero page addressing mode.
        /// </summary>
        /// <returns>1 if an extra cycle was used, 0 otherwise</returns>
        public static byte ZeroPage()
        {
            // Set the absolute address to the data at the program counter
            // and increment the program counter
            CPU.addressAbsolute = CPU.Read(Registers.PC++);

            // The zero page addressing mode only uses the first byte of the
            // address, so we need to mask the address to 0x00FF to get the
            // correct address.
            CPU.addressAbsolute &= 0x00FF;

            // This mode never uses an extra cycle
            return 0;
        }

        /// <summary>
        /// The zero page X addressing mode.
        /// </summary>
        /// <returns>1 if an extra cycle was used, 0 otherwise</returns>
        public static byte ZeroPageX()
        {
            // Set the absolute address to the data at the program counter + X
            CPU.addressAbsolute = (ushort)(CPU.Read(Registers.PC++) + Registers.X);

            // The zero page addressing mode only uses the first byte of the
            // address, so we need to mask the address to 0x00FF to get the
            // correct address.
            CPU.addressAbsolute &= 0x00FF;
            return 0;
        }

        /// <summary>
        /// The zero page Y addressing mode.
        /// </summary>
        /// <returns>1 if an extra cycle was used, 0 otherwise</returns>
        public static byte ZeroPageY()
        {
            // Set the absolute address to the data at the program counter + Y
            CPU.addressAbsolute = (ushort)(CPU.Read(Registers.PC++) + Registers.Y);

            // The zero page addressing mode only uses the first byte of the
            // address, so we need to mask the address to 0x00FF to get the
            // correct address.
            CPU.addressAbsolute &= 0x00FF;
            return 0;
        }

        /// <summary>
        /// The relative addressing mode.
        /// </summary>
        /// <returns>1 if an extra cycle was used, 0 otherwise</returns>
        public static byte Relative()
        {
            // Set the relative address to the data at the program counter
            CPU.addressRelative = CPU.Read(Registers.PC++);

            // If the relative address is negative, we need to sign extend it
            // to get the correct address.
            if ((CPU.addressRelative & 0x80) != 0)
            {
                CPU.addressRelative |= 0xFF00;
            }

            // This mode never uses an extra cycle
            return 0;
        }

        /// <summary>
        /// The absolute addressing mode.
        /// </summary>
        /// <returns>1 if an extra cycle was used, 0 otherwise</returns>
        public static byte Absolute()
        {
            // Get the low byte of the address, and increment the program counter
            ushort lo = CPU.Read(Registers.PC++);

            // Get the high byte of the address
            ushort hi = CPU.Read(Registers.PC++);

            // Set the absolute address to the high byte shifted left 8 bits OR'd with the low byte
            CPU.addressAbsolute = (ushort)((hi << 8) | lo);

            // This mode never uses an extra cycle
            return 0;
        }

        /// <summary>
        /// The absolute X addressing mode.
        /// </summary>
        /// <returns>1 if an extra cycle was used, 0 otherwise</returns>
        public static byte AbsoluteX()
        {
            // Get the low byte of the address, and increment the program counter
            ushort lo = CPU.Read(Registers.PC++);

            // Get the high byte of the address
            ushort hi = CPU.Read(Registers.PC++);

            // Set the absolute address to the high byte shifted left 8 bits OR'd with the low byte
            CPU.addressAbsolute = (ushort)((hi << 8) | lo);
            // Add the X register to the absolute address
            CPU.addressAbsolute += Registers.X;

            // If the absolute address crosses a page boundary, we need to add an extra cycle
            if ((CPU.addressAbsolute & 0xFF00) != (hi << 8))
            {
                return 1;
            }

            return 0;
        }

        /// <summary>
        /// The absolute Y addressing mode.
        /// </summary>
        /// <returns>1 if an extra cycle was used, 0 otherwise</returns>
        public static byte AbsoluteY()
        {
            // Get the low byte of the address, and increment the program counter
            ushort lo = CPU.Read(Registers.PC++);

            // Get the high byte of the address
            ushort hi = CPU.Read(Registers.PC++);

            // Set the absolute address to the high byte shifted left 8 bits OR'd with the low byte
            CPU.addressAbsolute = (ushort)((hi << 8) | lo);
            // Add the Y register to the absolute address
            CPU.addressAbsolute += Registers.Y;

            // If the absolute address crosses a page boundary, we need to add an extra cycle
            if ((CPU.addressAbsolute & 0xFF00) != (hi << 8))
            {
                return 1;
            }

            return 0;
        }

        /// <summary>
        /// The indirect addressing mode.
        /// </summary>
        /// <returns>1 if an extra cycle was used, 0 otherwise</returns>
        public static byte Indirect()
        {
            // Get the low byte of the address, and increment the program counter
            ushort ptrLo = CPU.Read(Registers.PC++);
            // Get the high byte of the address, and increment the program counter
            ushort ptrHi = CPU.Read(Registers.PC++);

            // Set the pointer address to the high byte shifted left 8 bits OR'd with the low byte
            ushort ptr = (ushort)((ptrHi << 8) | ptrLo);

            // If the low byte of the pointer is 0xFF, we need to simulate a bug in the 6502
            // where the high byte of the address is fetched from the low byte of the pointer
            // and the high byte of the address is fetched from the low byte of the pointer + 1.
            // Otherwise, we just get the high byte of the address from the pointer + 1.
            if (ptrLo == 0x00FF)
            {
                CPU.addressAbsolute = (ushort)((CPU.Read((ushort)(ptr & 0xFF00)) << 8) | CPU.Read((ushort)(ptr + 0)));
            }
            else
            {
                CPU.addressAbsolute = (ushort)((CPU.Read((ushort)(ptr + 1)) << 8) | CPU.Read((ushort)(ptr + 0)));
            }

            // This mode never uses an extra cycle
            return 0;

        }

        /// <summary>
        /// The indirect X addressing mode.
        /// </summary>
        /// <returns>1 if an extra cycle was used, 0 otherwise</returns>
        public static byte IndirectX()
        {
            // Store the address of the pointer and increment the program counter
            ushort ptr = CPU.Read(Registers.PC++);

            // Get the low byte of the address
            ushort lo = CPU.Read((ushort)((ptr + Registers.X) & 0x00FF));
            // Get the high byte of the address
            ushort hi = CPU.Read((ushort)((ptr + Registers.X + 1) & 0x00FF));

            // Set the indirect address to the high byte shifted left 8 bits OR'd with the low byte
            CPU.addressAbsolute = (ushort)((hi << 8) | lo);

            // This mode never uses an extra cycle
            return 0;
        }

        /// <summary>
        /// The indirect y addressing mode.
        /// </summary>
        /// <returns>1 if an extra cycle was used, 0 otherwise</returns>
        public static byte IndirectY()
        {
            // Store the address of the pointer and increment the program counter
            ushort ptr = CPU.Read(Registers.PC++);

            // Get the low byte of the address
            ushort lo = CPU.Read((ushort)(ptr & 0x00FF));
            // Get the high byte of the address
            ushort hi = CPU.Read((ushort)((ptr + 1) & 0x00FF));

            // Set the indirect address to the high byte shifted left 8 bits OR'd with the low byte
            CPU.addressAbsolute = (ushort)((hi << 8) | lo);
            // Add the Y register to the indirect address
            CPU.addressAbsolute += Registers.Y;

            // If the indirect address crosses a page boundary, we need to add an extra cycle
            if ((CPU.addressAbsolute & 0xFF00) != (hi << 8))
            {
                return 1;
            }

            return 0;
        }
    }
}