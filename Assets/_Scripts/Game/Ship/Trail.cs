using System.Collections.Generic;
using UnityEngine;

namespace CosmicShore.Core
{
	// TODO: Couldn't this be replaced by a simple list?
	public class Trail
	{
		public List<TrailBlock> TrailList { get; }
		// TODO: if this type is replaced by a list, the indices could just be a value of TrailBlock.
		readonly Dictionary<TrailBlock, int> trailBlockIndices;

		public Trail(bool isLoop = false)
		{
			TrailList = new();
			trailBlockIndices = new Dictionary<TrailBlock, int>();
		}

		public void Add(TrailBlock block)
		{
			trailBlockIndices.Add(block, TrailList.Count);
			TrailList.Add(block);
			block.Index = block.TrailBlockProperties.Index = trailBlockIndices.Count;
		}


		public TrailBlock GetBlock(int blockIndex)
		{
			if (blockIndex < 0)
			{
				return TrailList[0];
			}
			return TrailList[blockIndex];
		}
	}
}