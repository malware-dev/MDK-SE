using System;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System.Threading.Tasks;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

// Thanks, Digi, for providing me with this class.
// A minimum of alteration on my part to match my specific needs in this utility.

namespace Digi.BuildInfo.Features.LiveData
{
    public struct TempBlockSpawn
    {
        public static void Spawn(MyCubeBlockDefinition def, bool deleteGridOnSpawn = true, Action<IMySlimBlock> callback = null, Vector3D? spawnPos = null)
        {
            new TempBlockSpawn(def, deleteGridOnSpawn, callback, spawnPos);
        }

        readonly bool _deleteGrid;
        readonly MyCubeBlockDefinition _blockDef;
        readonly Action<IMySlimBlock> _callback;

        TempBlockSpawn(MyCubeBlockDefinition def, bool deleteGridOnSpawn = true, Action<IMySlimBlock> callback = null, Vector3D? spawnAt = null)
        {
            _blockDef = def;
            _deleteGrid = deleteGridOnSpawn;
            _callback = callback;

            MatrixD camMatrix = MyAPIGateway.Session.Camera.WorldMatrix;
            var spawnPos = spawnAt ?? camMatrix.Translation + camMatrix.Backward * 100;

            MyObjectBuilder_CubeBlock blockOB = CreateBlockOB(def.Id);

            MyObjectBuilder_CubeGrid gridOB = new MyObjectBuilder_CubeGrid()
            {
                CreatePhysics = false,
                GridSizeEnum = def.CubeSize,
                PositionAndOrientation = new MyPositionAndOrientation(spawnPos, Vector3.Forward, Vector3.Up),
                PersistentFlags = MyPersistentEntityFlags2.InScene,
                IsStatic = true,
                Editable = false,
                DestructibleBlocks = false,
                IsRespawnGrid = false,
                Name = "BuildInfo_TemporaryGrid",
            };

            gridOB.CubeBlocks.Add(blockOB);

            // not really required for a single grid.
            //MyAPIGateway.Entities.RemapObjectBuilder(gridOB);

            MyCubeGrid grid = (MyCubeGrid)MyAPIGateway.Entities.CreateFromObjectBuilderParallel(gridOB, true, SpawnCompleted);
            grid.DisplayName = grid.Name;
            grid.IsPreview = true;
            grid.Save = false;
            grid.Flags = EntityFlags.None;
            grid.Render.Visible = false;
        }

        MyObjectBuilder_CubeBlock CreateBlockOB(MyDefinitionId defId)
        {
            MyObjectBuilder_CubeBlock blockObj = (MyObjectBuilder_CubeBlock)MyObjectBuilderSerializer.CreateNewObject(defId);

            blockObj.EntityId = 0;
            blockObj.Min = Vector3I.Zero;

#if false
            // NOTE these types do not check if their fields are null in their Remap() method.
            MyObjectBuilder_TimerBlock timer = blockObj as MyObjectBuilder_TimerBlock;
            if(timer != null)
            {
                timer.Toolbar = BuildInfoMod.Instance.Caches.EmptyToolbarOB;
                return blockObj;
            }

            MyObjectBuilder_ButtonPanel button = blockObj as MyObjectBuilder_ButtonPanel;
            if(button != null)
            {
                button.Toolbar = BuildInfoMod.Instance.Caches.EmptyToolbarOB;
                return blockObj;
            }

            MyObjectBuilder_SensorBlock sensor = blockObj as MyObjectBuilder_SensorBlock;
            if(sensor != null)
            {
                sensor.Toolbar = BuildInfoMod.Instance.Caches.EmptyToolbarOB;
                return blockObj;
            }

            // prohibited...
            //MyObjectBuilder_TargetDummyBlock targetDummy = blockObj as MyObjectBuilder_TargetDummyBlock;
            //if(targetDummy != null)
            //{
            //    targetDummy.Toolbar = BuildInfoMod.Instance.Caches.EmptyToolbarOB;
            //    return blockObj;
            //}
#endif

            return blockObj;
        }

        void SpawnCompleted(IMyEntity ent)
        {
            IMyCubeGrid grid = ent as IMyCubeGrid;

            try
            {
                IMySlimBlock block = grid?.GetCubeBlock(Vector3I.Zero);
                if (block == null)
                {
                    MyLog.Default.Error($"Can't get block from spawned entity for block: {_blockDef.Id.ToString()}; grid={grid?.EntityId.ToString() ?? "(NULL)"};)");
                    return;
                }

                _callback?.Invoke(block);
            }
            catch (Exception e)
            {
                MyLog.Default.Error(e.ToString());
            }
            finally
            {
                if (_deleteGrid && grid != null)
                {
                    grid.Close();
                }
            }
        }
    }
}