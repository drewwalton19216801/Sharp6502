namespace Sharp6502
{
    /// <summary>
    /// An instruction definition.
    /// </summary>
    public class Instruction
    {
        /// <summary>
        /// The name (mnemonic) of the instruction.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The opcode of the instruction.
        /// </summary>
        public byte Opcode { get; set; }

        /// <summary>
        /// the length of the instruction in bytes.
        /// </summary>
        public uint Length { get; set; }

        /// <summary>
        /// The number of cycles the instruction takes to execute.
        /// </summary>
        public byte Cycles { get; set; }

        /// <summary>
        /// The addressing mode of the instruction.
        /// </summary>
        public string AddressingMode { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Instruction"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="opcode">The opcode.</param>
        /// <param name="length">The length.</param>
        /// <param name="cycles">The cycles.</param>
        /// <param name="addressingMode">The addressing mode.</param>
        public Instruction(string name, byte opcode, uint length, byte cycles, string addressingMode)
        {
            Name = name;
            Opcode = opcode;
            Length = length;
            Cycles = cycles;
            AddressingMode = addressingMode;
        }
    }
}
