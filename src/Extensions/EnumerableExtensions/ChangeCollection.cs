﻿using System;
using System.Collections.Generic;
using System.Linq;
using OwlCore.Events;

namespace OwlCore.Extensions
{
    /// <summary>
    /// Enumerable-related extension methods.
    /// </summary>
    public static partial class EnumerableExtensions
    {
        /// <summary>
        /// Handles properly inserting and removing items from a collection, given a list of <see cref="CollectionChangedItem{TSourceItem}"/>s.
        /// </summary>
        /// <typeparam name="TSourceItem">The source type of the items being added or removed.</typeparam>
        /// <typeparam name="TTargetItem">The type of the items in the collection being modified.</typeparam>
        /// <param name="source">The collection to modify.</param>
        /// <param name="addedItems">The items to add to the collection.</param>
        /// <param name="removedItems">The items to remove from the collection.</param>
        /// <param name="onItemAdded">A callback to convert <typeparamref name="TSourceItem"/> to <typeparamref name="TTargetItem"/>.</param>
        public static void ChangeCollection<TSourceItem, TTargetItem>(this IList<TTargetItem> source, IReadOnlyList<CollectionChangedItem<TSourceItem>> addedItems, IReadOnlyList<CollectionChangedItem<TSourceItem>> removedItems, Func<CollectionChangedItem<TSourceItem>, TTargetItem> onItemAdded)
        {
            foreach (var item in removedItems)
            {
                source.RemoveAt(item.Index);
            }

            var sortedIndices = removedItems.Select(x => x.Index).ToList();
            sortedIndices.Sort();

            // If elements are removed before they are added, the added items may be inserted at the wrong index.
            // To compensate, we need to check how many items were removed before the current index and shift the insert position back by that amount.
            for (var i = 0; i < addedItems.Count; i++)
            {
                var item = addedItems[i];
                var insertOffset = item.Index;

                // Finds the last removed index where the value is less than current pos.
                // Quicker to do this by getting the first removed index where value is greater than current pos, minus 1 index.
                var closestPrecedingRemovedIndex = sortedIndices.FindIndex(x => x > i) - 1;

                // If found
                if (closestPrecedingRemovedIndex != -2)
                {
                    // Shift the insert position backwards by the number of items that were removed
                    insertOffset = closestPrecedingRemovedIndex * -1;
                }

                if (source.Count >= insertOffset)
                {
                    // Insert the item
                    source.InsertOrAdd(insertOffset, onItemAdded(item));
                }
            }
        }

        /// <summary>
        /// Handles properly inserting and removing items from a collection, given a list of <see cref="CollectionChangedItem{TSourceItem}"/>s.
        /// </summary>
        /// <remarks>
        /// Indexes are treated as though you're always modifying the original collection.
        /// <para/>
        /// This means, for example, if you add 2 items, index 10 and 50,
        /// the first item is inserted at 10 and the second item shifts to 51.
        /// </remarks>
        /// <typeparam name="TSourceItem">The source type of the items being added or removed.</typeparam>
        /// <param name="source">The collection to modify.</param>
        /// <param name="addedItems">The items to add to the collection.</param>
        /// <param name="removedItems">The items to remove from the collection.</param>
        public static void ChangeCollection<TSourceItem>(this IList<TSourceItem> source, IReadOnlyList<CollectionChangedItem<TSourceItem>> addedItems, IReadOnlyList<CollectionChangedItem<TSourceItem>> removedItems)
        {
            var removedIndicesSorted = removedItems.Select(x => x.Index).ToList();
            removedIndicesSorted.Sort();

            // Remove items from highest index to lowest to avoid OutOfRangeExceptions
            var removedIndicesHighestToLowest = removedIndicesSorted.ToList();
            removedIndicesHighestToLowest.Reverse();

            foreach (var item in removedIndicesHighestToLowest)
                source.RemoveAt(item);

            // Last to first so we don't shift item positions when inserting.
            var addedItemsSorted = addedItems.OrderByDescending(x => x.Index).ToList();

            // If elements are removed before they are added, the added items may be inserted at the wrong index.
            // To compensate, we need to check how many items were removed before the current index and shift the insert position back by that amount.
            // For example, if an item at index 10 was added, but items 1-3 were removed, we need to insert at index 7 instead.
            for (var i = 0; i < addedItemsSorted.Count; i++)
            {
                var item = addedItemsSorted[i];
                var insertOffset = item.Index;

                // Find the number of items removed before the index we're adding to.
                var precedingRemovedItemsCount = removedIndicesSorted.FindIndex(x => x >= item.Index);

                // If found
                if (precedingRemovedItemsCount >= 0)
                {
                    // Shift the insert position backwards by the number of items that were removed
                    insertOffset = item.Index - precedingRemovedItemsCount;
                }

                // Insert the item
                source.InsertOrAdd(insertOffset, item.Data);
            }
        }
    }
}