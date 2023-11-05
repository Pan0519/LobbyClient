using System;
using System.Collections.Generic;

namespace Lobby.Jigsaw
{
    public class JigsawAlbumData
    {
        public string albumId { get; }
        public List<JigsawPieceData> pieces { get; }
        public DateTime startedAt { get; }
        public DateTime endedAt { get; }

        public JigsawAlbumData(string albumId, List<JigsawPieceData> pieces, DateTime startedAt, DateTime endedAt)
        {
            this.albumId = albumId;
            this.pieces = pieces;
            this.startedAt = startedAt;
            this.endedAt = endedAt;
        }
    }

    public class JigsawAlbumProgress
    {
        public string albumId { get; }
        public int numCollected { get; }

        public JigsawAlbumProgress(string albumId, int numCollected)
        {
            this.albumId = albumId;
            this.numCollected = numCollected;
        }
    }
}
