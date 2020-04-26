namespace rotstein
{
    struct Tile
    {
        public TileKind kind;
    }

    enum TileKind
    {
        Void,
        Planks,
        Stone,
        Iron,
        RedstoneBlock,
    }
}
