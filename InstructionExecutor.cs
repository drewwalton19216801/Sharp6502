using System.Reflection;

namespace Sharp6502
{
    /// <summary>
    /// The instruction execution engine
    /// </summary>
    public static class InstructionExecutor
    {
        /// <summary>
        /// Executes the instruction.
        /// </summary>
        /// <returns>1 if an additional cycle was used, otherwise 0</returns>
        public static byte ExecuteInstruction()
        {
            // --------------------------------------
            // We're going to use reflection to get the method that corresponds to the instruction name
            // and then invoke it. This is a lot faster than a switch statement, and it's a lot easier
            // to maintain.
            // --------------------------------------

            // Get the instruction name
            string instructionName = CPU.CurrentInstruction?.Name ?? "XXX";

            // Get the method
            MethodInfo? method = typeof(InstructionExecutor).GetMethod(instructionName, BindingFlags.Public | BindingFlags.Static) ?? throw new InvalidOperationException($"The instruction \"{instructionName}\" does not exist.");

            // Invoke the method
            object? result = (byte?)method.Invoke(null, new object[] { });

            // Handle the result
            if (result is byte byteResult)
            {
                return byteResult;
            }
            else
            {
                throw new InvalidOperationException($"The instruction \"{instructionName}\" returned an invalid result.");
            }
        }

        /// <summary>
        /// The ADC (Add with Carry) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte ADC()
        {
            byte extraCycle = 0;

            // Fetch the data to add
            CPU.Fetch();

            // Add the value to the accumulator
            CPU.temp = (ushort)(Registers.A + CPU.fetchedByte + (Registers.GetFlag(CPUFlags.Carry) ? 1 : 0));

            // Set the zero flag if the result is 0
            Registers.SetFlag(CPUFlags.Zero, (CPU.temp & 0x00FF) == 0);

            // If the CPU variant is NOT NES_6502, then we check for decimal mode
            if (CPU.cpuVariant != CPU.Variant.NES_6502)
            {
                // If the decimal flag is set, then we need to convert the result to BCD
                if (Registers.GetFlag(CPUFlags.Decimal))
                {
                    // If the result is greater than 99, then we need to add 96 to the result
                    if (((Registers.A & 0xF) + (CPU.fetchedByte & 0xF) + (Registers.GetFlag(CPUFlags.Carry) ? 1 : 0)) > 9)
                    {
                        CPU.temp += 6;
                    }

                    // Set the negative flag if the result is negative
                    Registers.SetFlag(CPUFlags.Negative, (CPU.temp & 0x80) != 0);

                    // Set the overflow flag if the result is greater than 127 or less than -128
                    Registers.SetFlag(CPUFlags.Overflow, ((Registers.A ^ CPU.temp) & (CPU.fetchedByte ^ CPU.temp) & 0x80) != 0);

                    // If the result is greater than 99, then we need to add 96 to the result
                    if (CPU.temp > 99)
                    {
                        CPU.temp += 96;
                    }

                    // Set the carry flag if the result is greater than 0x99
                    Registers.SetFlag(CPUFlags.Carry, CPU.temp > 0x99);

                    // Since we used decimal mode, we need to consume an extra cycle
                    extraCycle = 1;
                }
            }
            else
            {
                // Set the negative flag if the result is less than 0
                Registers.SetFlag(CPUFlags.Negative, (CPU.temp & 0x80) != 0);

                // Set the overflow flag if the result is greater than 127 or less than -128
                Registers.SetFlag(CPUFlags.Overflow, ((Registers.A ^ CPU.temp) & (CPU.fetchedByte ^ CPU.temp) & 0x80) != 0);

                // Set the carry flag to the opposite of (CPU.temp > 0xFF)
                Registers.SetFlag(CPUFlags.Carry, CPU.temp > 0xFF);
            }

            // Store the result in the accumulator
            Registers.A = (byte)(CPU.temp & 0x00FF);

            // This instruction can take an extra cycle
            return extraCycle;
        }

        /// <summary>
        /// The AND (AND Memory with Accumulator) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte AND()
        {
            return 0;
        }

        /// <summary>
        /// The ASL (Arithmetic Shift Left) instruction for memory.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte ASL()
        {
            // Fetch the next byte
            CPU.Fetch();

            // Shift the fetched byte left by 1
            CPU.temp = (ushort)(CPU.fetchedByte << 1);

            // Set the carry flag if the result is greater than 255
            Registers.SetFlag(CPUFlags.Carry, (CPU.temp & 0xFF00) > 0);

            // Set the negative flag if the result is less than 0
            Registers.SetFlag(CPUFlags.Negative, (CPU.temp & 0x80) != 0);

            // Set the zero flag if the result is 0
            Registers.SetFlag(CPUFlags.Zero, (CPU.temp & 0x00FF) == 0);

            // Otherwise, we need to store the result in memory
            CPU.Write(CPU.addressAbsolute, (byte)(CPU.temp & 0x00FF));

            // This instruction does not take an extra cycle
            return 0;
        }

        /// <summary>
        /// The ASL (Arithmetic Shift Left) instruction for the accumulator.
        /// </summary>
        /// <returns>A byte.</returns>
        public static byte ASLA()
        {
            // Get the value of the accumulator
            CPU.temp = (ushort)(Registers.A);

            // Set the carry flag
            Registers.SetFlag(CPUFlags.Carry, (CPU.temp & 0x80) != 0);

            // Shift the accumulator left by 1
            CPU.temp <<= 1;

            // Mask the value to 8 bits
            CPU.temp &= 0xFF;

            // Set the negative flag if the result is less than 0
            Registers.SetFlag(CPUFlags.Negative, (CPU.temp & 0x80) != 0);

            // Set the zero flag if the result is 0
            Registers.SetFlag(CPUFlags.Zero, (CPU.temp & 0x00FF) == 0);

            // Store the result in the accumulator
            Registers.A = (byte)(CPU.temp & 0x00FF);

            // This instruction does not take an extra cycle
            return 0;
        }

        /// <summary>
        /// The BCC (Branch if Carry Clear) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte BCC()
        {
            return 0;
        }

        /// <summary>
        /// The BCS (Branch if Carry Set) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte BCS()
        {
            return 0;
        }

        /// <summary>
        /// The BEQ (Branch if Equal to Zero) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte BEQ()
        {
            return 0;
        }

        /// <summary>
        /// The BIT (Test Bits in Memory with Accumulator) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte BIT()
        {
            return 0;
        }

        /// <summary>
        /// The BMI (Branch if Minus) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte BMI()
        {
            return 0;
        }

        /// <summary>
        /// The BNE (Branch if Not Equal to Zero) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte BNE()
        {
            return 0;
        }

        /// <summary>
        /// The BPL (Branch if Positive) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte BPL()
        {
            return 0;
        }

        /// <summary>
        /// The BRK (Force Break) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte BRK()
        {
            // Increment the PC
            Registers.PC++;

            // Set the interrupt flag
            Registers.SetFlag(CPUFlags.InterruptDisable, true);

            // Push the PC to the stack (high byte first)
            CPU.PushByte((byte)(Registers.PC >> 8 & 0x00FF));
            // Push the PC to the stack (low byte last)
            CPU.PushByte((byte)(Registers.PC & 0x00FF));

            // Set the break flag
            Registers.SetFlag(CPUFlags.Break, true);

            // Push the status register to the stack
            CPU.PushByte(Registers.P);

            // Clear the break flag
            Registers.SetFlag(CPUFlags.Break, false);

            // Set the PC to the data at the interrupt vector
            Registers.PC = (ushort)(Memory.Read(0xFFFE) | Memory.Read(0xFFFF) << 8);

            // Return 0 cycles
            return 0;
        }

        /// <summary>
        /// The BVC (Branch if Overflow Clear) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte BVC()
        {
            return 0;
        }

        /// <summary>
        /// The BVS (Branch if Overflow Set) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte BVS()
        {
            return 0;
        }

        /// <summary>
        /// The CLC (Clear Carry Flag) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte CLC()
        {
            // Clear the carry flag
            Registers.SetFlag(CPUFlags.Carry, false);

            // Return 0 extra cycles
            return 0;
        }

        /// <summary>
        /// The CLD (Clear Decimal Mode) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte CLD()
        {
            // Clear the decimal flag
            Registers.SetFlag(CPUFlags.Decimal, false);

            // Return 0 extra cycles
            return 0;
        }

        /// <summary>
        /// The CLI (Clear Interrupt Disable Bit) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte CLI()
        {
            // Clear the interrupt disable flag
            Registers.SetFlag(CPUFlags.InterruptDisable, false);

            // Return 0 extra cycles
            return 0;
        }

        /// <summary>
        /// The CLV (Clear Overflow Flag) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte CLV()
        {
            // Clear the overflow flag
            Registers.SetFlag(CPUFlags.Overflow, false);

            // Return 0 extra cycles
            return 0;
        }

        /// <summary>
        /// The CMP (Compare Memory with Accumulator) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte CMP()
        {
            // Fetch the next byte
            CPU.Fetch();

            // Compare the fetched byte with the accumulator
            CPU.temp = (ushort)(Registers.A - CPU.fetchedByte);

            // Set the carry flag if the accumulator is greater than or equal to the fetched byte
            Registers.SetFlag(CPUFlags.Carry, Registers.A >= CPU.fetchedByte);

            // Set the Zero flag if the lower 8 bits of CPU.temp are equal to zero after the CMP instruction.
            Registers.SetFlag(CPUFlags.Zero, (CPU.temp & 0x00FF) == 0x0000);

            // Set the Negative flag if bit 7 of CPU.temp is set
            Registers.SetFlag(CPUFlags.Negative, (CPU.temp & 0x0080) == 0x0080);

            // Return 1 extra cycle
            return 1;
        }

        /// <summary>
        /// The CPX (Compare Memory and Index X) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte CPX()
        {
            // Fetch the next byte
            CPU.Fetch();

            // Compare the fetched byte with the X register
            CPU.temp = (ushort)(Registers.X - CPU.fetchedByte);

            // Set the carry flag if the X register is greater than or equal to the fetched byte
            Registers.SetFlag(CPUFlags.Carry, Registers.X >= CPU.fetchedByte);

            // Set the Zero flag if the lower 8 bits of CPU.temp are equal to zero after the CMP instruction.
            Registers.SetFlag(CPUFlags.Zero, (CPU.temp & 0x00FF) == 0x0000);

            // Set the Negative flag if bit 7 of CPU.temp is set
            Registers.SetFlag(CPUFlags.Negative, (CPU.temp & 0x0080) == 0x0080);

            // Return 1 extra cycle
            return 1;
        }

        /// <summary>
        /// The CPY (Compare Memory and Index Y) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte CPY()
        {
            // Fetch the next byte
            CPU.Fetch();

            // Compare the fetched byte with the Y register
            CPU.temp = (ushort)(Registers.Y - CPU.fetchedByte);

            // Set the carry flag if the Y register is greater than or equal to the fetched byte
            Registers.SetFlag(CPUFlags.Carry, Registers.Y >= CPU.fetchedByte);

            // Set the Zero flag if the lower 8 bits of CPU.temp are equal to zero after the CMP instruction.
            Registers.SetFlag(CPUFlags.Zero, (CPU.temp & 0x00FF) == 0x0000);

            // Set the Negative flag if bit 7 of CPU.temp is set
            Registers.SetFlag(CPUFlags.Negative, (CPU.temp & 0x0080) == 0x0080);

            // Return 1 extra cycle
            return 1;
        }

        /// <summary>
        /// The DEC (Decrement Memory by One) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte DEC()
        {
            // Fetch the next byte
            CPU.Fetch();

            // Decrement the fetched byte by one
            CPU.temp = (ushort)(CPU.fetchedByte - 1);

            // Write the decremented byte back to memory
            CPU.Write(CPU.addressAbsolute, (byte)(CPU.temp & 0x00FF));

            // Set the zero flag
            Registers.SetFlag(CPUFlags.Zero, (CPU.temp & 0x00FF) == 0x0000);

            // Set the negative flag
            Registers.SetFlag(CPUFlags.Negative, (CPU.temp & 0x0080) == 0x0080);

            // Return 0 extra cycles
            return 0;
        }

        /// <summary>
        /// The DEX (Decrement Index X by One) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte DEX()
        {
            // Decrement the X register by one
            Registers.X--;

            // Set the zero flag
            Registers.SetFlag(CPUFlags.Zero, Registers.X == 0x00);

            // Set the negative flag
            Registers.SetFlag(CPUFlags.Negative, (Registers.X & 0x80) == 0x80);

            // Return 0 extra cycles
            return 0;
        }

        /// <summary>
        /// The DEY (Decrement Index Y by One) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte DEY()
        {
            // Decrement the Y register by one\
            Registers.Y--;

            // Set the zero flag
            Registers.SetFlag(CPUFlags.Zero, Registers.Y == 0x00);

            // Set the negative flag
            Registers.SetFlag(CPUFlags.Negative, (Registers.Y & 0x80) == 0x80);

            // Return 0 extra cycles
            return 0;
        }

        /// <summary>
        /// The EOR (Exclusive-OR Memory with Accumulator) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte EOR()
        {
            return 0;
        }

        /// <summary>
        /// The INC (Increment Memory by One) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte INC()
        {
            // Fetch the data
            CPU.Fetch();

            // Increment the data
            CPU.temp = (ushort)(CPU.fetchedByte + 1);

            // Write the data back to memory
            Memory.Write(CPU.addressAbsolute, (byte)(CPU.temp & 0x00FF));

            // Set the zero flag if the data is zero
            Registers.SetFlag(CPUFlags.Zero, (CPU.temp & 0x00FF) == 0x0000);

            // Set the negative flag if bit 7 is set
            Registers.SetFlag(CPUFlags.Negative, (CPU.temp & 0x0080) > 0);

            // We didn't use an extra cycle
            return 0;
        }

        /// <summary>
        /// The INX (Increment Index X by One) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte INX()
        {
            // Increment the X register
            Registers.X++;

            // Set the zero flag if the X register is zero
            Registers.SetFlag(CPUFlags.Zero, Registers.X == 0x00);

            // Set the negative flag if bit 7 is set
            Registers.SetFlag(CPUFlags.Negative, (Registers.X & 0x80) > 0);

            // We didn't use an extra cycle
            return 0;
        }

        /// <summary>
        /// The INY (Increment Index Y by One) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte INY()
        {
            // Increment the Y register
            Registers.Y++;

            // Set the zero flag if the Y register is zero
            Registers.SetFlag(CPUFlags.Zero, Registers.Y == 0x00);

            // Set the negative flag if bit 7 is set
            Registers.SetFlag(CPUFlags.Negative, (Registers.Y & 0x80) > 0);

            // We didn't use an extra cycle
            return 0;
        }

        /// <summary>
        /// The JMP (Jump to New Location) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte JMP()
        {
            // Set the PC to the absolute address of the operand
            Registers.PC = CPU.addressAbsolute;

            // Return 0 because the instruction did not use an extra cycle
            return 0;
        }

        /// <summary>
        /// The JSR (Jump to New Location Saving Return Address) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte JSR()
        {
            // Decrement the PC by 1
            Registers.PC--;

            // Push the high byte of the PC to the stack
            CPU.PushByte((byte)((Registers.PC >> 8) & 0x00FF));

            // Push the low byte of the PC to the stack
            CPU.PushByte((byte)(Registers.PC & 0x00FF));

            // Set the PC to the absolute address of the operand
            Registers.PC = CPU.addressAbsolute;

            // Return 0 because the instruction did not use an extra cycle
            return 0;
        }

        /// <summary>
        /// The LDA (Load Accumulator with Memory) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte LDA()
        {
            // Fetch the next byte from memory
            CPU.Fetch();

            // Load the fetched byte into the accumulator
            Registers.A = CPU.fetchedByte;

            // Set the zero flag if the accumulator is zero
            Registers.SetFlag(CPUFlags.Zero, Registers.A == 0);

            // Set the negative flag if the accumulator is negative
            Registers.SetFlag(CPUFlags.Negative, (Registers.A & 0x80) > 0);

            // Return 1 since this instruction uses an extra cycle
            return 1;
        }

        /// <summary>
        /// The LDX (Load Index X with Memory) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte LDX()
        {
            // Fetch the next byte from memory
            CPU.Fetch();

            // Load the fetched byte into the X register
            Registers.X = CPU.fetchedByte;

            // Set the zero flag if the X register is zero
            Registers.SetFlag(CPUFlags.Zero, Registers.X == 0);

            // Set the negative flag if the X register is negative
            Registers.SetFlag(CPUFlags.Negative, (Registers.X & 0x80) > 0);

            // Return 1 since this instruction uses an extra cycle
            return 1;
        }

        /// <summary>
        /// The LDY (Load Index Y with Memory) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte LDY()
        {
            // Fetch the next byte from memory
            CPU.Fetch();

            // Load the fetched byte into the Y register
            Registers.Y = CPU.fetchedByte;

            // Set the zero flag if the Y register is zero
            Registers.SetFlag(CPUFlags.Zero, Registers.Y == 0);

            // Set the negative flag if the Y register is negative
            Registers.SetFlag(CPUFlags.Negative, (Registers.Y & 0x80) > 0);

            // Return 1 since this instruction uses an extra cycle
            return 1;
        }

        /// <summary>
        /// The LSR (Shift One Bit Right (Memory or Accumulator)) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte LSR()
        {
            return 0;
        }

        /// <summary>
        /// The NOP (No Operation) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte NOP()
        {
            return 0;
        }

        /// <summary>
        /// The ORA (OR Memory with Accumulator) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte ORA()
        {
            return 0;
        }

        /// <summary>
        /// The PHA (Push Accumulator on Stack) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte PHA()
        {
            return 0;
        }

        /// <summary>
        /// The PHP (Push Processor Status on Stack) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte PHP()
        {
            return 0;
        }

        /// <summary>
        /// The PLA (Pull Accumulator from Stack) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte PLA()
        {
            return 0;
        }

        /// <summary>
        /// The PLP (Pull Processor Status from Stack) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte PLP()
        {
            return 0;
        }

        /// <summary>
        /// The ROL (Rotate One Bit Left (Accumulator) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte ROLA()
        {
            return 0;
        }

        /// <summary>
        /// The ROL (Rotate One Bit Left (Memory) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte ROL()
        {
            return 0;
        }

        /// <summary>
        /// The ROR (Rotate One Bit Right (Accumulator) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        /// <remarks>This version operates on the accumulator.</remarks>
        public static byte RORA()
        {
            return CPU.cpuVariant switch
            {
                CPU.Variant.NMOS_6502 => RORA_NMOS(), // The original 6502 (NMOS) has a bug in the ROR instruction
                CPU.Variant.CMOS_65C02 => RORA_CMOS(), // The CMOS 6502 does not have the bug in the ROR instruction
                _ => RORA_CMOS(), // By default, use the CMOS 6502 implementation
            };
        }

        /// <summary>
        /// The ROR (Rotate One Bit Right (Memory) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        /// <remarks>This is the version that operates on memory.</remarks>
        public static byte ROR()
        {
            return CPU.cpuVariant switch
            {
                CPU.Variant.NMOS_6502 => ROR_NMOS(), // The original 6502 (NMOS) has a bug in the ROR instruction
                CPU.Variant.CMOS_65C02 => ROR_CMOS(), // The CMOS 6502 does not have the bug in the ROR instruction
                _ => ROR_CMOS(), // By default, use the CMOS 6502 implementation
            };
        }

        /// <summary>
        /// ROR (Rotate One Bit Right (Accumulator)) instruction for the NMOS 6502.
        /// </summary>
        /// <returns>A byte.</returns>
        /// <remarks>
        /// The NMOS 6502 has a bug in the ROR instruction. It shifts left instead of right,
        /// shifts a 0 into the 9th bit (instead of the carry flag), and does not affect the
        /// carry flag.
        /// </remarks>
        private static byte RORA_NMOS()
        {
            // Load the accumulator into the temp variable
            CPU.temp = Registers.A;

            // Set the 9th bit of the temp variable to 0
            CPU.temp &= 0x7F;

            // Shift the temp variable left by 1
            CPU.temp <<= 1;

            // Mask the temp variable to 8 bits
            CPU.temp &= 0xFF;

            // Set the negative flag if the 8th bit of the temp variable is set
            Registers.SetFlag(CPUFlags.Negative, (CPU.temp & 0x80) > 0);

            // Set the zero flag if the temp variable is zero
            Registers.SetFlag(CPUFlags.Zero, CPU.temp == 0);

            if (CPU.CurrentInstruction?.AddressingMode == "Immediate")
            {
                // Store the temp variable into the accumulator
                Registers.A = (byte)CPU.temp;
            }
            else
            {
                // Store the temp variable into memory
                CPU.Write(CPU.addressAbsolute, (byte)CPU.temp);
            }

            // Return 0 since this instruction does not use an extra cycle
            return 0;
        }

        /// <summary>
        /// ROR (Rotate One Bit Right (Accumulator) instruction for the CMOS 6502.
        /// </summary>
        /// <returns>A byte.</returns>
        private static byte RORA_CMOS()
        {
            // Load the accumulator into the temp variable
            CPU.temp = Registers.A;

            // If the carry flag is set, set the 9th bit of the temp variable
            if (Registers.GetFlag(CPUFlags.Carry))
            {
                CPU.temp |= 0x100;
            }

            // Set the carry flag if the 9th bit of the temp variable is set
            Registers.SetFlag(CPUFlags.Carry, (CPU.temp & 0x01) > 0);

            // Shift the temp variable right by 1
            CPU.temp >>= 1;

            // Mask the temp variable to 8 bits
            CPU.temp &= 0xFF;

            // Set the negative flag if the 8th bit of the temp variable is set
            Registers.SetFlag(CPUFlags.Negative, (CPU.temp & 0x80) > 0);

            // Set the zero flag if the temp variable is zero
            Registers.SetFlag(CPUFlags.Zero, CPU.temp == 0);

            // Store the temp variable into the accumulator
            Registers.A = (byte)CPU.temp;

            // Return 0 since this instruction does not use an extra cycle
            return 0;
        }

        /// <summary>
        /// ROR (Rotate One Bit Right (Memory) instruction for the NMOS 6502.
        /// </summary>
        /// <returns>A byte.</returns>
        /// <remarks>
        /// The NMOS 6502 has a bug in the ROR instruction. It shifts left instead of right,
        /// shifts a 0 into the 9th bit (instead of the carry flag), and does not affect the
        /// carry flag.
        /// </remarks>
        private static byte ROR_NMOS()
        {
            // Load the next byte from memory into the temp variable
            CPU.temp = CPU.Fetch();

            // Set the 9th bit of the temp variable to 0
            CPU.temp &= 0x7F;

            // Shift the temp variable left by 1
            CPU.temp <<= 1;

            // Mask the temp variable to 8 bits
            CPU.temp &= 0xFF;

            // Set the negative flag if the 8th bit of the temp variable is set
            Registers.SetFlag(CPUFlags.Negative, (CPU.temp & 0x80) > 0);

            // Set the zero flag if the temp variable is zero
            Registers.SetFlag(CPUFlags.Zero, CPU.temp == 0);

            // Store the temp variable into memory
            CPU.Write(CPU.addressAbsolute, (byte)CPU.temp);

            // Return 0 since this instruction does not use an extra cycle
            return 0;
        }

        /// <summary>
        /// ROR (Rotate One Bit Right (Memory) instruction for the CMOS 6502.
        /// </summary>
        /// <returns>A byte.</returns>
        private static byte ROR_CMOS()
        {
            // Load the next byte from memory into the temp variable
            CPU.temp = CPU.Fetch();

            // If the carry flag is set, set the 9th bit of the temp variable
            if (Registers.GetFlag(CPUFlags.Carry))
            {
                CPU.temp |= 0x100;
            }

            // Set the carry flag if the 9th bit of the temp variable is set
            Registers.SetFlag(CPUFlags.Carry, (CPU.temp & 0x01) > 0);

            // Shift the temp variable right by 1
            CPU.temp >>= 1;

            // Mask the temp variable to 8 bits
            CPU.temp &= 0xFF;

            // Set the negative flag if the 8th bit of the temp variable is set
            Registers.SetFlag(CPUFlags.Negative, (CPU.temp & 0x80) > 0);

            // Set the zero flag if the temp variable is zero
            Registers.SetFlag(CPUFlags.Zero, CPU.temp == 0);

            // Store the temp variable into memory
            CPU.Write(CPU.addressAbsolute, (byte)CPU.temp);

            // Return 0 since this instruction does not use an extra cycle
            return 0;
        }

        /// <summary>
        /// The RTI (Return from Interrupt) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte RTI()
        {
            // Increment the stack pointer
            Registers.SP++;

            // Status register is pulled from the stack
            Registers.P = CPU.PopByte();

            // Clear the break flag
            Registers.SetFlag(CPUFlags.Break, false);

            // Clear the unused flag
            Registers.SetFlag(CPUFlags.Unused, true);

            // Program counter is pulled from the stack
            Registers.PC = CPU.PopWord();

            // Return 0 since this instruction does not use an extra cycle
            return 0;
        }

        /// <summary>
        /// The RTS (Return from Subroutine) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte RTS()
        {
            // Pop a word from the stack and store it in the program counter
            Registers.PC = CPU.PopWord();

            // Increment the program counter
            Registers.PC++;

            // Return 0 since this instruction does not use an extra cycle
            return 0;
        }

        /// <summary>
        /// The SBC (Subtract Memory from Accumulator with Borrow) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte SBC()
        {
            // Set the extra cycle to 0. This will be set to 1 if decimal mode is used.
            byte extraCycle = 0;

            // Fetch the next byte from memory
            CPU.Fetch();

            // Perform the subtraction using two's complement
            CPU.temp = (ushort)(Registers.A - CPU.fetchedByte - (Registers.GetFlag(CPUFlags.Carry) ? 0 : 1));

            // Set the zero flag if the result is 0
            Registers.SetFlag(CPUFlags.Zero, (CPU.temp & 0x00FF) == 0);

            // If the CPU variant is NOT NES_6502, then we check for decimal mode
            if (CPU.cpuVariant != CPU.Variant.NES_6502)
            {
                // If the decimal flag is set, then we need to convert the result to BCD
                if (Registers.GetFlag(CPUFlags.Decimal))
                {
                    // Adjust the result if necessary
                    if (((Registers.A & 0xF) - (CPU.fetchedByte & 0xF) - (Registers.GetFlag(CPUFlags.Carry) ? 0 : 1)) < 0)
                    {
                        CPU.temp -= 6;
                    }

                    // Set the negative flag if the result is negative
                    Registers.SetFlag(CPUFlags.Negative, (CPU.temp & 0x80) != 0);

                    // Set the overflow flag if the result is greater than 127 or less than -128
                    Registers.SetFlag(CPUFlags.Overflow, ((Registers.A ^ CPU.temp) & (~CPU.fetchedByte ^ CPU.temp) & 0x80) != 0);

                    // Adjust the result if necessary
                    if (CPU.temp > 0x99)
                    {
                        CPU.temp -= 96;
                    }

                    // Set the carry flag if the result is less than or equal to 0x99
                    Registers.SetFlag(CPUFlags.Carry, CPU.temp <= 0x99);

                    // We use an extra cycle here
                    extraCycle = 1;
                }
            }
            else
            {
                // Set the negative flag if the result is negative
                Registers.SetFlag(CPUFlags.Negative, (CPU.temp & 0x80) != 0);

                // Set the overflow flag if the result is greater than 127 or less than -128
                Registers.SetFlag(CPUFlags.Overflow, ((Registers.A ^ CPU.temp) & (~CPU.fetchedByte ^ CPU.temp) & 0x80) != 0);

                // Set the carry flag to the opposite of (CPU.temp > 0xFF)
                Registers.SetFlag(CPUFlags.Carry, CPU.temp <= 0xFF);
            }

            // Store the result in the accumulator
            Registers.A = (byte)(CPU.temp & 0x00FF);

            // This instruction can take an extra cycle
            return extraCycle;
        }

        /// <summary>
        /// The SEC (Set Carry Flag) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte SEC()
        {
            // Set the carry flag
            Registers.SetFlag(CPUFlags.Carry, true);

            // No extra cycle
            return 0;
        }

        /// <summary>
        /// The SED (Set Decimal Flag) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte SED()
        {
            // Set the decimal flag
            Registers.SetFlag(CPUFlags.Decimal, true);

            // No extra cycle
            return 0;
        }

        /// <summary>
        /// The SEI (Set Interrupt Disable Status) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte SEI()
        {
            // Set the interrupt disable flag
            Registers.SetFlag(CPUFlags.InterruptDisable, true);

            // No extra cycle
            return 0;
        }

        /// <summary>
        /// The STA (Store Accumulator in Memory) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte STA()
        {
            // Write the accumulator to memory
            Memory.Write(CPU.addressAbsolute, Registers.A);

            // No extra cycle
            return 0;
        }

        /// <summary>
        /// The STX (Store Index X in Memory) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte STX()
        {
            // Write the X register to memory
            Memory.Write(CPU.addressAbsolute, Registers.X);

            // No extra cycle
            return 0;
        }

        /// <summary>
        /// The STY (Store Index Y in Memory) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte STY()
        {
            // Write the Y register to memory
            Memory.Write(CPU.addressAbsolute, Registers.Y);

            // No extra cycle
            return 0;
        }

        /// <summary>
        /// The TAX (Transfer Accumulator to Index X) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte TAX()
        {
            // Copy the accumulator to the X register
            Registers.X = Registers.A;

            // Set the zero flag if the result is zero
            Registers.SetFlag(CPUFlags.Zero, Registers.X == 0x00);

            // Set the negative flag if bit 7 is set
            Registers.SetFlag(CPUFlags.Negative, (Registers.X & 0x80) > 0);

            // No extra cycle
            return 0;
        }

        /// <summary>
        /// The TAY (Transfer Accumulator to Index Y) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte TAY()
        {
            // Copy the accumulator to the Y register
            Registers.Y = Registers.A;

            // Set the zero flag if the result is zero
            Registers.SetFlag(CPUFlags.Zero, Registers.Y == 0x00);

            // Set the negative flag if bit 7 is set
            Registers.SetFlag(CPUFlags.Negative, (Registers.Y & 0x80) > 0);

            // No extra cycle
            return 0;
        }

        /// <summary>
        /// The TSX (Transfer Stack Pointer to Index X) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte TSX()
        {
            // Copy the stack pointer to the X register
            Registers.X = Registers.SP;

            // Set the zero flag if the result is zero
            Registers.SetFlag(CPUFlags.Zero, Registers.X == 0x00);

            // Set the negative flag if bit 7 is set
            Registers.SetFlag(CPUFlags.Negative, (Registers.X & 0x80) > 0);

            // No extra cycle
            return 0;
        }

        /// <summary>
        /// The TXA (Transfer Index X to Accumulator) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte TXA()
        {
            // Copy the X register to the accumulator
            Registers.A = Registers.X;

            // Set the zero flag if the result is zero
            Registers.SetFlag(CPUFlags.Zero, Registers.A == 0x00);

            // Set the negative flag if bit 7 is set
            Registers.SetFlag(CPUFlags.Negative, (Registers.A & 0x80) > 0);

            // No extra cycle
            return 0;
        }

        /// <summary>
        /// The TXS (Transfer Index X to Stack Pointer) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte TXS()
        {
            // Copy the X register to the stack pointer
            Registers.SP = Registers.X;

            // No extra cycle
            return 0;
        }

        /// <summary>
        /// The TYA (Transfer Index Y to Accumulator) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte TYA()
        {
            // Copy the Y register to the accumulator
            Registers.A = Registers.Y;

            // Set the zero flag if the result is zero
            Registers.SetFlag(CPUFlags.Zero, Registers.A == 0x00);

            // Set the negative flag if bit 7 is set
            Registers.SetFlag(CPUFlags.Negative, (Registers.A & 0x80) > 0);

            // No extra cycle
            return 0;
        }

        /// <summary>
        /// The XXX (Unofficial Opcode) instruction.
        /// </summary>
        /// <returns>1 if the instruction used an extra cycle, otherwise 0</returns>
        public static byte XXX()
        {
            return 0;
        }
    }
}