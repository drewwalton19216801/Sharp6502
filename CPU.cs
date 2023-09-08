namespace Sharp6502
{
    /// <summary>
    /// The 6502 microprocessor.
    /// </summary>
    public static class CPU
    {
        /// <summary>
        /// The CPU variant (such as 6502, 65C02, etc).
        /// </summary>
        public enum Variant
        {
            /// <summary>
            /// The original 6502.
            /// </summary>
            NMOS_6502,

            /// <summary>
            /// The 65C02.
            /// </summary>
            CMOS_65C02,

            /// <summary>
            /// The Ricoh 2A03 (aka NES 6502).
            /// </summary>
            NES_6502,
        }

        /// <summary>
        /// The CPU execution state.
        /// </summary>
        public enum ExecutionState
        {
            /// <summary>
            /// The CPU is stopped.
            /// </summary>
            Stopped,

            /// <summary>
            /// The CPU is fetching data.
            /// </summary>
            Fetching,

            /// <summary>
            /// The CPU is executing an instruction.
            /// </summary>
            Executing,

            /// <summary>
            /// The CPU has received an interrupt.
            /// </summary>
            Interrupt,

            /// <summary>
            /// The CPU has encountered an illegal opcode.
            /// </summary>
            IllegalOpcode
        }

        /// <summary>
        /// The number of cycles remaining for the current instruction.
        /// </summary>
        public static byte cycles = 0;

        /// <summary>
        /// A temporary variable used by the CPU.
        /// </summary>
        public static ushort temp = 0x0000;

        /// <summary>
        /// The absolute address fetched.
        /// </summary>
        public static ushort addressAbsolute = 0x0000;

        /// <summary>
        /// The relative address fetched.
        /// </summary>
        public static ushort addressRelative = 0x00;

        /// <summary>
        /// The current opcode.
        /// </summary>
        public static byte opcode = 0x00;

        /// <summary>
        /// The current execution state.
        /// </summary>
        public static ExecutionState cpuState = ExecutionState.Stopped;

        /// <summary>
        /// The current CPU variant.
        /// </summary>
        /// <remarks>
        /// This defaults to CMOS_65C02, which is the most compatible variant, but
        /// can be switched mid-execution. Why you would want to do this is beyond me.
        /// </remarks>
        public static Variant cpuVariant = Variant.CMOS_65C02;

        /// <summary>
        /// The fetched byte.
        /// </summary>
        public static byte fetchedByte = 0x00;

        /// <summary>
        /// The string representation of the current instruction.
        /// </summary>
        public static string currentDisassembly = string.Empty;

        /// <summary>
        /// The CPU lock object.
        /// </summary>
        public static object cpuLock = new();

        /// <summary>
        /// Gets or sets the current instruction.
        /// </summary>
        public static Instruction? CurrentInstruction { get; set; }

        /// <summary>
        /// Reads a byte from memory.
        /// </summary>
        /// <param name="address">The address to read from.</param>
        /// <returns>The data.</returns>
        public static byte Read(ushort address)
        {
            return Memory.Read(address);
        }

        /// <summary>
        /// Reads a word from memory.
        /// </summary>
        /// <param name="address">The address to read from.</param>
        /// <returns>The data.</returns>
        public static ushort ReadWord(ushort address)
        {
            return (ushort)(Read(address) | (Read((ushort)(address + 1)) << 8));
        }

        /// <summary>
        /// Writes a byte to memory.
        /// </summary>
        /// <param name="address">The address to write to.</param>
        /// <param name="data">The data to write.</param>
        public static void Write(ushort address, byte data)
        {
            Memory.Write(address, data);
        }

        /// <summary>
        /// Runs the CPU for one clock cycle.
        /// </summary>
        public static void Clock()
        {
            /*
             * Instructions can require a variable number of clock cycles to execute. The number of cycles required is stored in the instruction's
             * definition. The number of cycles remaining is stored in the CPU's cycles field. When the cycles field reaches zero, the instruction
             * is complete and the next instruction can be fetched.
             */
            if (cycles == 0)
            {
                // Set the CPU state to fetching
                cpuState = ExecutionState.Fetching;

                // Fetch the next instruction
                opcode = Read(Registers.PC++);

                // Always set the unused flag to 1.
                Registers.SetFlag(CPUFlags.Unused, true);

                // Decode the instruction
                CurrentInstruction = InstructionSet.Decode(opcode);

                // Get the number of cycles required to execute the instruction
                cycles = CurrentInstruction.Cycles;

                // Set the CPU state to executing
                cpuState = ExecutionState.Executing;

                // Update the current disassembly
                currentDisassembly = Disassemble(CurrentInstruction);

                // Run the addressing mode method and get the number of additional cycles required
                byte addressingUsedExtraCycle = AddressingModes.GetAddress(CurrentInstruction.AddressingMode);

                // Run the instruction method and get the number of additional cycles required
                byte instructionUsedExtraCycle = InstructionExecutor.ExecuteInstruction();

                // Add the additional cycles to the total number of cycles required
                cycles += (byte)(addressingUsedExtraCycle & instructionUsedExtraCycle);

                // Set the unused flag to 1
                Registers.SetFlag(CPUFlags.Unused, true);
            }

            // Decrement the number of cycles remaining for this instruction
            cycles--;
        }

        /// <summary>
        /// Emits an interrupt request.
        /// </summary>
        /// <remarks>
        /// IRQs are a complicated operation. The current instruction is allowed to finish, but then the current program counter and status register
        /// are pushed to the stack. The interrupt vector is then read from memory and the program counter is set to the interrupt vector. The interrupt
        /// flag is set to prevent further interrupts from being processed. When the interrupt handler is complete, the status register and program
        /// counter are restored to their previous values and execution continues as normal.
        /// </remarks>
        public static void IRQ()
        {
            // Make sure interrupts are enabled
            if (Registers.GetFlag(CPUFlags.InterruptDisable) == false)
            {
                // Push the program counter to the stack
                Write((ushort)(0x0100 + Registers.SP), (byte)((Registers.PC >> 8) & 0x00FF));
                Registers.SP--;
                Write((ushort)(0x0100 + Registers.SP), (byte)(Registers.PC & 0x00FF));
                Registers.SP--;

                // Push the status register to the stack
                Registers.SetFlag(CPUFlags.Break, false);
                Registers.SetFlag(CPUFlags.Unused, true);
                Registers.SetFlag(CPUFlags.InterruptDisable, true);
                Write((ushort)(0x0100 + Registers.SP), Registers.P);
                Registers.SP--;

                // Set the interrupt vector
                Registers.PC = ReadWord(0xFFFE);

                // Set the CPU state to interrupt
                cpuState = ExecutionState.Interrupt;

                // Add the number of cycles required to execute the interrupt
                cycles += 7;
            }
        }

        /// <summary>
        /// Emits a non-maskable interrupt request.
        /// </summary>
        /// <remarks>
        /// A non-maskable interrupt is similar to a regular IRQ, but it cannot be ignored. It
        /// acts like a regular IRQ, but reads the new PC from memory location 0xFFFA instead of
        /// 0xFFFE.
        /// </remarks>
        public static void NMI()
        {
            // Push the program counter to the stack
            Write((ushort)(0x0100 + Registers.SP), (byte)((Registers.PC >> 8) & 0x00FF));
            Registers.SP--;
            Write((ushort)(0x0100 + Registers.SP), (byte)(Registers.PC & 0x00FF));
            Registers.SP--;

            // Set the break flag to 0, unused and interrupt to 1
            Registers.SetFlag(CPUFlags.Break, false);
            Registers.SetFlag(CPUFlags.Unused, true);
            Registers.SetFlag(CPUFlags.InterruptDisable, true);

            // Push the status register to the stack
            Write((ushort)(0x0100 + Registers.SP), Registers.P);

            // Decrement the stack pointer
            Registers.SP--;

            // Set the interrupt vector
            Registers.PC = ReadWord(0xFFFA);
            cpuState = ExecutionState.Interrupt;

            // Add the number of cycles required to execute the interrupt
            cycles += 8;
        }

        /// <summary>
        /// Pushes a byte to the stack
        /// </summary>
        /// <param name="data">The data.</param>
        public static void PushByte(byte data)
        {
            Write((ushort)(0x0100 + Registers.SP), data);
            Registers.SP--;
        }

        /// <summary>
        /// Pops a byte from the stack.
        /// </summary>
        /// <returns>A byte.</returns>
        public static byte PopByte()
        {
            Registers.SP++;
            return Read((ushort)(0x0100 + Registers.SP));
        }

        /// <summary>
        /// Pops a word from the stack.
        /// </summary>
        /// <returns>An ushort.</returns>
        public static ushort PopWord()
        {
            ushort word = PopByte();
            word |= (ushort)(PopByte() << 8);
            return word;
        }

        /// <summary>
        /// Fetches the next byte from memory.
        /// </summary>
        /// <returns>The data.</returns>
        /// <remarks>
        /// This method fetches the next byte from memory. If the current instruction is an implied instruction, no byte is fetched.
        /// </remarks>
        public static byte Fetch()
        {
            if (CurrentInstruction == null)
            {
                throw new InvalidOperationException("Cannot fetch instruction when no instruction is set.");
            }
            else if (!(CurrentInstruction.AddressingMode == "Implied"))
            {
                fetchedByte = Read(addressAbsolute);
            }
            return fetchedByte;
        }

        /// <summary>
        /// Resets the CPU to its initial power-up state.
        /// </summary>
        public static void Reset()
        {
            Registers.A = 0;
            Registers.X = 0;
            Registers.Y = 0;
            Registers.SP = 0xFF;
            Registers.PC = ReadWord(0xFFFC); // Read the PC from the reset vector.
            Registers.P = (byte)(CPUFlags.None | CPUFlags.Unused | CPUFlags.InterruptDisable);
            cycles = 8;
        }

        /// <summary>
        /// Disassembles the instruction at the specified address.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <returns>Ths disassembled instruction.</returns>
        public static string Disassemble(ushort address)
        {
            // Read the instruction at the specified address.
            byte opcode = Read(address);

            // Get the instruction.
            Instruction instruction = InstructionSet.Decode(opcode);

            // Return the disassembled instruction.
            return Disassemble(instruction);
        }

        /// <summary>
        /// Disassembles a range of addresses.
        /// </summary>
        /// <param name="startAddress">The address to begin disassembling from.</param>
        /// <param name="count">The number of addresses to disassemble</param>
        /// <returns>An array of string.</returns>
        public static string[] Disassemble(ushort startAddress, int count)
        {
            /*
             * We need to disassemble the instructions in the range of addresses specified by startAddress and count.
             * 
             * We'll do this by creating a list of strings, and adding each disassembled instruction to the list. We
             * need to keep track of the current address, and increment it by the number of bytes in the instruction,
             * as well as not trying to disassemble operands.
             * 
             * Non-instruction bytes will return a string containing the word "DATA"
             */
            ushort address = startAddress;
            Instruction? previousInstruction = null;
            List<string> instructions = new();

            // Loop through the addresses.
            for (int i = 0; i < count; i++)
            {
                // Check if we're disassembling an operand.
                if (previousInstruction != null && previousInstruction.AddressingMode == "Immediate")
                {
                    // We're disassembling an operand, so we just add "DATA" to the list.
                    instructions.Add($"DATA");
                }

                // Read the instruction at the current address.
                byte opcode = Read(address);

                // Get the instruction.
                Instruction instruction = InstructionSet.Decode(opcode);

                // Disassemble the instruction.
                string disassembledInstruction = Disassemble(instruction);
                instructions.Add(disassembledInstruction);

                // Go through the addressing mode and add the operand to the instruction.
                switch (instruction.AddressingMode)
                {
                    case "Immediate":
                        {
                            // Get the operand.
                            byte operand = Read((ushort)(address + 1));

                            // Add the operand to the instruction.
                            instructions[^1] += $" #{operand:X2}";
                            break;
                        }
                    case "ZeroPage":
                        {
                            // Get the operand.
                            byte operand = Read((ushort)(address + 1));

                            // Add the operand to the instruction.
                            instructions[^1] += $" ${operand:X2}";
                            break;
                        }
                    case "ZeroPageX":
                        {
                            // Get the operand.
                            byte operand = Read((ushort)(address + 1));

                            // Add the operand to the instruction.
                            instructions[^1] += $" ${operand:X2},X";
                            break;
                        }
                    case "ZeroPageY":
                        {
                            // Get the operand.
                            byte operand = Read((ushort)(address + 1));

                            // Add the operand to the instruction.
                            instructions[^1] += $" ${operand:X2},Y";
                            break;
                        }
                    case "Absolute":
                        {
                            // Get the operand.
                            ushort operand = ReadWord((ushort)(address + 1));

                            // Add the operand to the instruction.
                            instructions[^1] += $" ${operand:X4}";
                            break;
                        }
                    case "AbsoluteX":
                        {
                            // Get the operand.
                            ushort operand = ReadWord((ushort)(address + 1));

                            // Add the operand to the instruction.
                            instructions[^1] += $" ${operand:X4},X";
                            break;
                        }
                    case "AbsoluteY":
                        {
                            // Get the operand.
                            ushort operand = ReadWord((ushort)(address + 1));

                            // Add the operand to the instruction.
                            instructions[^1] += $" ${operand:X4},Y";
                            break;
                        }
                    case "Indirect":
                        {
                            // Get the operand.
                            ushort operand = ReadWord((ushort)(address + 1));

                            // Add the operand to the instruction.
                            instructions[^1] += $" (${operand:X4})";
                            break;
                        }
                    case "IndirectX":
                        {
                            // Get the operand.
                            byte operand = Read((ushort)(address + 1));

                            // Add the operand to the instruction.
                            instructions[^1] += $" (${operand:X2},X)";
                            break;
                        }
                    case "IndirectY":
                        {
                            // Get the operand.
                            byte operand = Read((ushort)(address + 1));

                            // Add the operand to the instruction.
                            instructions[^1] += $" (${operand:X2}),Y";
                            break;
                        }
                    case "Relative":
                        {
                            // Get the operand.
                            byte operand = Read((ushort)(address + 1));

                            // Add the operand to the instruction.
                            instructions[^1] += $" ${operand:X2}";
                            break;
                        }
                }

                // Increment the address by the number of bytes in the instruction.
                address += (ushort)instruction.Length;

                // Store the instruction and address.
                previousInstruction = instruction;
            }

            // Return the list of disassembled instructions.
            return instructions.ToArray();
        }

        /// <summary>
        /// Disassembles an instruction.
        /// </summary>
        /// <param name="instruction">The instruction.</param>
        /// <returns>The disassembled instruction.</returns>
        public static string Disassemble(Instruction instruction)
        {
            // Get the instruction's address mode.
            string addressingMode = instruction.AddressingMode;

            ushort operand;
            string operandString = string.Empty;

            // Get the string representation of the instruction and its operand.
            switch (addressingMode)
            {
                case "Implied":
                    {
                        operandString = string.Empty;
                        break;
                    }
                case "Immediate":
                    {
                        operand = Read((ushort)(Registers.PC));
                        operandString = $" #{operand:X2}";
                        break;
                    }
                case "ZeroPage":
                    {
                        operand = Read((ushort)(Registers.PC));
                        operandString = $" ${operand:X2}";
                        break;
                    }
                case "ZeroPageX":
                    {
                        operand = Read((ushort)(Registers.PC));
                        operandString = $" ${operand:X2},X";
                        break;
                    }
                case "ZeroPageY":
                    {
                        operand = Read((ushort)(Registers.PC));
                        operandString = $" ${operand:X2},Y";
                        break;
                    }
                case "Relative":
                    {
                        operand = Read((ushort)(Registers.PC));
                        operandString = $" ${operand:X2}";
                        break;
                    }
                case "Absolute":
                    {
                        operand = ReadWord((ushort)(Registers.PC));
                        operandString = $" ${operand:X4}";
                        break;
                    }
                case "AbsoluteX":
                    {
                        operand = ReadWord((ushort)(Registers.PC));
                        operandString = $" ${operand:X4},X";
                        break;
                    }
                case "AbsoluteY":
                    {
                        operand = ReadWord((ushort)(Registers.PC));
                        operandString = $" ${operand:X4},Y";
                        break;
                    }
                case "Indirect":
                    {
                        operand = ReadWord((ushort)(Registers.PC));
                        operandString = $" (${operand:X4})";
                        break;
                    }
                case "IndirectX":
                    {
                        operand = Read((ushort)(Registers.PC));
                        operandString = $" (${operand:X2},X)";
                        break;
                    }
                case "IndirectY":
                    {
                        operand = Read((ushort)(Registers.PC));
                        operandString = $" (${operand:X2}),Y";
                        break;
                    }
            }

            // Return the disassembled instruction.
            return $"{instruction.Name} {operandString}";
        }
    }
}