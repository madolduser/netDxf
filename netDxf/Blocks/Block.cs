#region netDxf library, Copyright (C) 2009-2018 Daniel Carvajal (haplokuon@gmail.com)

//                        netDxf library
// Copyright (C) 2009-2018 Daniel Carvajal (haplokuon@gmail.com)
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

#endregion

using System;
using System.Collections.Generic;
using netDxf.Collections;
using netDxf.Entities;
using netDxf.Header;
using netDxf.Objects;
using netDxf.Tables;

namespace netDxf.Blocks
{
    /// <summary>
    /// Represents a block definition.
    /// </summary>
    public class Block :
        TableObject
    {
        #region delegates and events

        public delegate void LayerChangedEventHandler(Block sender, TableObjectChangedEventArgs<Layer> e);

        public event LayerChangedEventHandler LayerChanged;

        protected virtual Layer OnLayerChangedEvent(Layer oldLayer, Layer newLayer)
        {
            LayerChangedEventHandler ae = this.LayerChanged;
            if (ae != null)
            {
                TableObjectChangedEventArgs<Layer> eventArgs = new TableObjectChangedEventArgs<Layer>(oldLayer, newLayer);
                ae(this, eventArgs);
                return eventArgs.NewValue;
            }
            return newLayer;
        }

        public delegate void EntityAddedEventHandler(Block sender, BlockEntityChangeEventArgs e);

        public event EntityAddedEventHandler EntityAdded;

        protected virtual void OnEntityAddedEvent(EntityObject item)
        {
            EntityAddedEventHandler ae = this.EntityAdded;
            if (ae != null)
                ae(this, new BlockEntityChangeEventArgs(item));
        }

        public delegate void EntityRemovedEventHandler(Block sender, BlockEntityChangeEventArgs e);

        public event EntityRemovedEventHandler EntityRemoved;

        protected virtual void OnEntityRemovedEvent(EntityObject item)
        {
            EntityRemovedEventHandler ae = this.EntityRemoved;
            if (ae != null)
                ae(this, new BlockEntityChangeEventArgs(item));
        }

        public delegate void AttributeDefinitionAddedEventHandler(Block sender, BlockEntityChangeEventArgs e);

        public event AttributeDefinitionAddedEventHandler AttributeDefinitionAdded;

        protected virtual void OnAttributeDefinitionAddedEvent(EntityObject item)
        {
            AttributeDefinitionAddedEventHandler ae = this.AttributeDefinitionAdded;
            if (ae != null)
                ae(this, new BlockEntityChangeEventArgs(item));
        }

        public delegate void AttributeDefinitionRemovedEventHandler(Block sender, BlockEntityChangeEventArgs e);

        public event AttributeDefinitionRemovedEventHandler AttributeDefinitionRemoved;

        protected virtual void OnAttributeDefinitionRemovedEvent(EntityObject item)
        {
            AttributeDefinitionRemovedEventHandler ae = this.AttributeDefinitionRemoved;
            if (ae != null)
                ae(this, new BlockEntityChangeEventArgs(item));
        }

        #endregion

        #region private fields

        private readonly EntityCollection entities;
        private readonly AttributeDefinitionDictionary attributes;
        private string description;
        private readonly EndBlock end;
        private BlockTypeFlags flags;
        private Layer layer;
        private Vector3 origin;
        private readonly bool readOnly;
        private readonly string xrefFile;

        #endregion

        #region constants

        /// <summary>
        /// Default ModelSpace block name.
        /// </summary>
        public const string DefaultModelSpaceName = "*Model_Space";

        /// <summary>
        /// Default PaperSpace block name.
        /// </summary>
        public const string DefaultPaperSpaceName = "*Paper_Space";

        /// <summary>
        /// Gets the default *Model_Space block.
        /// </summary>
        public static Block ModelSpace
        {
            get { return new Block(DefaultModelSpaceName, null, null, false); }
        }

        /// <summary>
        /// Gets the default *Paper_Space block.
        /// </summary>
        public static Block PaperSpace
        {
            get { return new Block(DefaultPaperSpaceName, null, null, false); }
        }

        #endregion

        #region constructors

        /// <summary>
        /// Initializes a new instance of the <c>Block</c> class as an external reference drawing. 
        /// </summary>
        /// <param name="name">Block name.</param>
        /// <param name="xrefFile">External reference path name.</param>
        /// <remarks>Only DWG files can be used as externally referenced blocks.</remarks>
        public Block(string name, string xrefFile)
            : this(name, xrefFile, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <c>Block</c> class as an external reference drawing. 
        /// </summary>
        /// <param name="name">Block name.</param>
        /// <param name="xrefFile">External reference path name.</param>
        /// <param name="overlay">Specifies if the external reference is an overlay, by default it is set to false.</param>
        /// <remarks>Only DWG files can be used as externally referenced blocks.</remarks>
        public Block(string name, string xrefFile, bool overlay)
            : this(name, null, null, true)
        {
            if (string.IsNullOrEmpty(xrefFile))
                throw new ArgumentNullException(nameof(xrefFile));

            this.readOnly = true;
            this.xrefFile = xrefFile;
            this.flags = BlockTypeFlags.XRef | BlockTypeFlags.ResolvedExternalReference;
            if (overlay)
                this.flags |= BlockTypeFlags.XRefOverlay;
        }

        /// <summary>
        /// Initializes a new instance of the <c>Block</c> class.
        /// </summary>
        /// <param name="name">Block name.</param>
        public Block(string name)
            : this(name, null, null, true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <c>Block</c> class.
        /// </summary>
        /// <param name="name">Block name.</param>
        /// <param name="entities">The list of entities that make the block.</param>
        public Block(string name, IEnumerable<EntityObject> entities)
            : this(name, entities, null, true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <c>Block</c> class.
        /// </summary>
        /// <param name="name">Block name.</param>
        /// <param name="entities">The list of entities that make the block.</param>
        /// <param name="attributes">The list of attribute definitions that make the block.</param>
        public Block(string name, IEnumerable<EntityObject> entities, IEnumerable<AttributeDefinition> attributes)
            : this(name, entities, attributes, true)
        {
        }

        internal Block(string name, IEnumerable<EntityObject> entities, IEnumerable<AttributeDefinition> attributes, bool checkName)
            : base(name, DxfObjectCode.Block, checkName)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            this.IsReserved = string.Equals(name, DefaultModelSpaceName, StringComparison.OrdinalIgnoreCase);
            this.readOnly = this.IsReserved || name.StartsWith(DefaultPaperSpaceName, StringComparison.OrdinalIgnoreCase);

            this.description = string.Empty;
            this.origin = Vector3.Zero;
            this.layer = Layer.Default;
            this.xrefFile = string.Empty;
            this.Owner = new BlockRecord(name);
            this.flags = BlockTypeFlags.None;
            this.end = new EndBlock(this);

            this.entities = new EntityCollection();
            this.entities.BeforeAddItem += this.Entities_BeforeAddItem;
            this.entities.AddItem += this.Entities_AddItem;
            this.entities.BeforeRemoveItem += this.Entities_BeforeRemoveItem;
            this.entities.RemoveItem += this.Entities_RemoveItem;
            if (entities != null) this.entities.AddRange(entities);

            this.attributes = new AttributeDefinitionDictionary();
            this.attributes.BeforeAddItem += this.AttributeDefinitions_BeforeAddItem;
            this.attributes.AddItem += this.AttributeDefinitions_ItemAdd;
            this.attributes.BeforeRemoveItem += this.AttributeDefinitions_BeforeRemoveItem;
            this.attributes.RemoveItem += this.AttributeDefinitions_RemoveItem;
            if (attributes != null) this.attributes.AddRange(attributes);
        }

        #endregion

        #region public properties

        /// <summary>
        /// Gets the name of the table object.
        /// </summary>
        /// <remarks>Table object names are case insensitive.</remarks>
        public new string Name
        {
            get { return base.Name; }
            set
            {
                if (base.Name.StartsWith("*"))
                    throw new ArgumentException("Blocks for internal use cannot be renamed.", nameof(value));
                base.Name = value;
                this.Record.Name = value;
            }
        }

        /// <summary>
        /// Gets or sets the block description.
        /// </summary>
        public string Description
        {
            get { return this.description; }
            set
            {
                if (this.readOnly)
                    return;
                this.description = string.IsNullOrEmpty(value) ? string.Empty : value;
            }
        }

        /// <summary>
        /// Gets or sets the block origin in world coordinates.
        /// </summary>
        /// <remarks>Always keep this value to the default Vector3.Zero.</remarks>
        public Vector3 Origin
        {
            get { return this.origin; }
            set
            {
                if (this.readOnly)
                    return;
                this.origin = value;
            }
        }

        /// <summary>
        /// Gets or sets the block <see cref="Layer">layer</see>.
        /// </summary>
        /// <remarks>It seems that the block layer is always the default "0" regardless of what is defined here, so it is pointless to change this value.</remarks>
        public Layer Layer
        {
            get { return this.layer; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                this.layer = this.OnLayerChangedEvent(this.layer, value);
            }
        }

        /// <summary>
        /// Gets the <see cref="EntityObject">entity</see> list of the block.
        /// </summary>
        /// <remarks>Null entities, attribute definitions or entities already owned by another block or document cannot be added to the list.</remarks>
        public EntityCollection Entities
        {
            get { return this.entities; }
        }

        /// <summary>
        /// Gets the <see cref="AttributeDefinition">entity</see> list of the block.
        /// </summary>
        /// <remarks>Null or attribute definitions already owned by another block or document cannot be added to the list.</remarks>
        public AttributeDefinitionDictionary AttributeDefinitions
        {
            get { return this.attributes; }
        }

        /// <summary>
        /// Gets the block record associated with this block.
        /// </summary>
        /// <remarks>It returns the same object as the owner property.</remarks>
        public BlockRecord Record
        {
            get { return (BlockRecord) this.Owner; }
        }

        /// <summary>
        /// Gets the block-type flags (bit-coded values, may be combined).
        /// </summary>
        public BlockTypeFlags Flags
        {
            get { return this.flags; }
            internal set { this.flags = value; }
        }

        /// <summary>
        /// Checks if the block should not be edited.
        /// </summary>
        /// <remarks>
        /// Any change made to a read only block will not be applied.
        /// This is the case of ModelSpace, PaperSpace, and externally referenced blocks.
        /// </remarks>
        public bool ReadOnly
        {
            get { return this.readOnly; }
        }

        /// <summary>
        /// Gets the external reference path name.
        /// </summary>
        /// <remarks>
        /// This property is only applicable to externally referenced blocks.
        /// </remarks>
        public string XrefFile
        {
            get { return this.xrefFile; }
        }

        /// <summary>
        /// Gets if the block is an external reference.
        /// </summary>
        public bool IsXRef
        {
            get { return this.flags.HasFlag(BlockTypeFlags.XRef); }
        }

        #endregion

        #region internal properties

        /// <summary>
        /// Gets or sets the block end object.
        /// </summary>
        internal EndBlock End
        {
            get { return this.end; }
        }

        #endregion

        #region public methods

        /// <summary>
        /// Creates a block from the content of a <see cref="DxfDocument">document</see>.
        /// </summary>
        /// <param name="doc">A <see cref="DxfDocument">DxfDocument</see> instance.</param>
        /// <param name="name">Name of the new block.</param>
        /// <returns>The block build from the <see cref="DxfDocument">document</see> content.</returns>
        /// <remarks>Only the entities contained in ModelSpace will make part of the block.</remarks>
        public static Block Create(DxfDocument doc, string name)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));

            Block block = new Block(name) {Origin = doc.DrawingVariables.InsBase};
            block.Record.Units = doc.DrawingVariables.InsUnits;
            IReadOnlyList<DxfObject> entities = doc.Layouts.GetReferences(Layout.ModelSpaceName);
            foreach (DxfObject dxfObject in entities)
            {
                EntityObject entity = dxfObject as EntityObject;
                if (entity == null)
                    continue;
                EntityObject clone = (EntityObject) entity.Clone();
                AttributeDefinition attdef = clone as AttributeDefinition;
                if (attdef != null)
                {
                    block.AttributeDefinitions.Add(attdef);
                    continue;
                }
                block.Entities.Add(clone);
            }
            return block;
        }

        /// <summary>
        /// Creates a block from an external dxf file.
        /// </summary>
        /// <param name="file">Dxf file name.</param>
        /// <returns>The block build from the dxf file content. It will return null if the file has not been able to load.</returns>
        /// <remarks>
        /// The name of the block will be the file name without extension, and
        /// only the entities contained in ModelSpace will make part of the block.
        /// </remarks>
        public static Block Load(string file)
        {
            return Load(file, null);
        }

        /// <summary>
        /// Creates a block from an external dxf file.
        /// </summary>
        /// <param name="file">Dxf file name.</param>
        /// <param name="name">Name of the new block.</param>
        /// <returns>The block build from the dxf file content. It will return null if the file has not been able to load.</returns>
        /// <remarks>Only the entities contained in ModelSpace will make part of the block.</remarks>
        public static Block Load(string file, string name)
        {
#if DEBUG
            DxfDocument dwg = DxfDocument.Load(file);
#else
            DxfDocument dwg;
            try 
            {
                dwg = DxfDocument.Load(file);
            }
            catch
            {
                return null;
            }
#endif

            string blkName = string.IsNullOrEmpty(name) ? dwg.Name : name;
            return Create(dwg, blkName);
        }

        /// <summary>
        /// Saves a block to a text dxf file.
        /// </summary>
        /// <param name="file">Dxf file name.</param>
        /// <param name="version">Version of the dxf database version.</param>
        /// <returns>Return true if the file has been successfully save, false otherwise.</returns>
        public bool Save(string file, DxfVersion version)
        {
            return this.Save(file, version, false);
        }

        /// <summary>
        /// Saves a block to a dxf file.
        /// </summary>
        /// <param name="file">Dxf file name.</param>
        /// <param name="version">Version of the dxf database version.</param>
        /// <param name="isBinary">Defines if the file will be saved as binary.</param>
        /// <returns>Return true if the file has been successfully save, false otherwise.</returns>
        public bool Save(string file, DxfVersion version, bool isBinary)
        {
            DxfDocument dwg = new DxfDocument(version);
            dwg.DrawingVariables.InsBase = this.origin;
            dwg.DrawingVariables.InsUnits = this.Record.Units;

            foreach (EntityObject entity in this.entities)
                dwg.AddEntity((EntityObject) entity.Clone());

            foreach (AttributeDefinition attdef in this.attributes.Values)
                dwg.AddEntity((EntityObject) attdef.Clone());

            return dwg.Save(file, isBinary);
        }

        #endregion

        #region overrides

        private static TableObject Clone(Block block, string newName, bool checkName)
        {
            if(block.ReadOnly)
                throw new ArgumentException("Read only blocks cannot be cloned.", nameof(block));

            Block copy = new Block(newName, null, null, checkName)
            {
                Description = block.description,
                Flags = block.flags,
                Layer = (Layer)block.Layer.Clone(),
                Origin = block.origin
            };

            // remove anonymous flag for renamed anonymous blocks
            if (checkName)
                copy.Flags &= ~BlockTypeFlags.AnonymousBlock;

            foreach (EntityObject e in block.entities)
                copy.entities.Add((EntityObject)e.Clone());

            foreach (AttributeDefinition a in block.attributes.Values)
                copy.attributes.Add((AttributeDefinition)a.Clone());

            foreach (XData data in block.XData.Values)
                copy.XData.Add((XData)data.Clone());

            foreach (XData data in block.Record.XData.Values)
                copy.Record.XData.Add((XData)data.Clone());

            return copy;
        }

        /// <summary>
        /// Creates a new Block that is a copy of the current instance.
        /// </summary>
        /// <param name="newName">Block name of the copy.</param>
        /// <returns>A new Block that is a copy of this instance.</returns>
        public override TableObject Clone(string newName)
        {
            return Clone(this, newName, true) ;
        }

        /// <summary>
        /// Creates a new Block that is a copy of the current instance.
        /// </summary>
        /// <returns>A new Block that is a copy of this instance.</returns>
        public override object Clone()
        {
            return Clone(this, this.Name, !this.flags.HasFlag(BlockTypeFlags.AnonymousBlock));
        }

        /// <summary>
        /// Assigns a handle to the object based in a integer counter.
        /// </summary>
        /// <param name="entityNumber">Number to assign.</param>
        /// <returns>Next available entity number.</returns>
        /// <remarks>
        /// Some objects might consume more than one, is, for example, the case of polylines that will assign
        /// automatically a handle to its vertexes. The entity number will be converted to an hexadecimal number.
        /// </remarks>
        internal override long AsignHandle(long entityNumber)
        {
            entityNumber = this.Owner.AsignHandle(entityNumber);
            entityNumber = this.end.AsignHandle(entityNumber);
            foreach (AttributeDefinition attdef in this.attributes.Values)
            {
                entityNumber = attdef.AsignHandle(entityNumber);
            }
            return base.AsignHandle(entityNumber);
        }

        #endregion

        #region Entities collection events

        private void Entities_BeforeAddItem(EntityCollection sender, EntityCollectionEventArgs e)
        {
            // null items, entities already owned by another Block, attribute definitions and attributes are not allowed in the entities list.
            if (e.Item == null)
                e.Cancel = true;
            else if (this.entities.Contains(e.Item))
                e.Cancel = true;
            else if (this.readOnly)
                e.Cancel = true;
            else if (e.Item is AttributeDefinition)
                e.Cancel = true;
            else if (e.Item.Owner != null)
            {
                // if the block does not belong to a document, all entities which owner is not null will be rejected
                if (this.Record.Owner == null)
                    e.Cancel = true;
                // if the block belongs to a document, the entity will be added to the block only if both, the block and the entity document, are the same
                // this is handled by the BlocksRecordCollection
            }
            else
                e.Cancel = false;
        }

        private void Entities_AddItem(EntityCollection sender, EntityCollectionEventArgs e)
        {
            if (this.readOnly)
                return;
            // if the entity is a hatch we will also add the boundary entities to the block
            if (e.Item.Type == EntityType.Hatch)
            {
                Hatch hatch = (Hatch) e.Item;
                foreach (HatchBoundaryPath path in hatch.BoundaryPaths)
                {
                    this.Hatch_BoundaryPathAdded(hatch, new ObservableCollectionEventArgs<HatchBoundaryPath>(path));
                }
                hatch.HatchBoundaryPathAdded += this.Hatch_BoundaryPathAdded;
                hatch.HatchBoundaryPathRemoved += this.Hatch_BoundaryPathRemoved;
            }
            this.OnEntityAddedEvent(e.Item);
            e.Item.Owner = this;
        }

        private void Entities_BeforeRemoveItem(EntityCollection sender, EntityCollectionEventArgs e)
        {
            // only items owned by the actual block can be removed
            e.Cancel = !ReferenceEquals(e.Item.Owner, this) || this.readOnly;
        }

        private void Entities_RemoveItem(EntityCollection sender, EntityCollectionEventArgs e)
        {
            if (this.readOnly)
                return;
            if (e.Item.Type == EntityType.Hatch)
            {
                Hatch hatch = (Hatch) e.Item;
                foreach (HatchBoundaryPath path in hatch.BoundaryPaths)
                {
                    this.Hatch_BoundaryPathRemoved(hatch, new ObservableCollectionEventArgs<HatchBoundaryPath>(path));
                }
                hatch.HatchBoundaryPathAdded -= this.Hatch_BoundaryPathAdded;
                hatch.HatchBoundaryPathRemoved -= this.Hatch_BoundaryPathRemoved;
            }
            this.OnEntityRemovedEvent(e.Item);
            e.Item.Owner = null;
        }

        private void Hatch_BoundaryPathAdded(Hatch sender, ObservableCollectionEventArgs<HatchBoundaryPath> e)
        {
            foreach (EntityObject entity in e.Item.Entities)
            {
                if (entity.Owner != null)
                {
                    if (!ReferenceEquals(entity.Owner, this))
                        throw new ArgumentException("The HatchBoundaryPath entity and the hatch must belong to the same block. Clone it instead.");
                }
                else
                {
                    this.Entities.Add(entity);
                }
            }
        }

        private void Hatch_BoundaryPathRemoved(Hatch sender, ObservableCollectionEventArgs<HatchBoundaryPath> e)
        {
            this.Entities.Remove(e.Item.Entities);
        }

        #endregion

        #region Attributes dictionary events

        private void AttributeDefinitions_BeforeAddItem(AttributeDefinitionDictionary sender, AttributeDefinitionDictionaryEventArgs e)
        {
            // attributes with the same tag, and attribute definitions already owned by another Block are not allowed in the attributes list.
            if (this.readOnly)
                e.Cancel = true;
            else if (e.Item.Owner != null)
            {
                // if the block does not belong to a document, all attribute definitions which owner is not null will be rejected
                if (this.Record.Owner == null)
                    e.Cancel = true;
                // if the block belongs to a document, the entity will be added to the block only if both, the block and the attribute definitions document, are the same
                // this is handled by the BlocksRecordCollection
            }
            else
                e.Cancel = false;
        }

        private void AttributeDefinitions_ItemAdd(AttributeDefinitionDictionary sender, AttributeDefinitionDictionaryEventArgs e)
        {
            if (this.readOnly)
                return;
            this.OnAttributeDefinitionAddedEvent(e.Item);
            e.Item.Owner = this;
            // the block has attributes
            this.flags |= BlockTypeFlags.NonConstantAttributeDefinitions;
        }

        private void AttributeDefinitions_BeforeRemoveItem(AttributeDefinitionDictionary sender, AttributeDefinitionDictionaryEventArgs e)
        {
            // only attribute definitions owned by the actual block can be removed
            e.Cancel = !ReferenceEquals(e.Item.Owner, this) || this.readOnly;
        }

        private void AttributeDefinitions_RemoveItem(AttributeDefinitionDictionary sender, AttributeDefinitionDictionaryEventArgs e)
        {
            if (this.readOnly)
                return;
            this.OnAttributeDefinitionRemovedEvent(e.Item);
            e.Item.Owner = null;
            if (this.attributes.Count == 0)
                this.flags &= ~BlockTypeFlags.NonConstantAttributeDefinitions;
        }

        #endregion
    }
}