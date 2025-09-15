namespace Content.Shared.Storage;

public static class StorageHelper
{
    public static Box2i GetBoundingBox(this IReadOnlyList<Box2i> boxes)
    {
        if (boxes.Count == 0)
            return new Box2i();

        var firstBox = boxes[0];

        if (boxes.Count == 1)
            return firstBox;

        var bottom = firstBox.Bottom;
        var left = firstBox.Left;
        var top = firstBox.Top;
        var right = firstBox.Right;

        for (var i = 1; i < boxes.Count; i++)
        {
            var box = boxes[i];

            if (bottom > box.Bottom)
                bottom = box.Bottom;

            if (left > box.Left)
                left = box.Left;

            if (top < box.Top)
                top = box.Top;

            if (right < box.Right)
                right = box.Right;
        }
        return new Box2i(left, bottom, right, top);
    }

    public static int GetArea(this IReadOnlyList<Box2i> boxes)
    {
        var area = 0;
        var bounding = boxes.GetBoundingBox();
        for (var y = bounding.Bottom; y <= bounding.Top; y++)
        {
            for (var x = bounding.Left; x <= bounding.Right; x++)
            {
                if (boxes.Contains(x, y))
                    area++;
            }
        }

        return area;
    }

    public static bool Contains(this IReadOnlyList<Box2i> boxes, int x, int y)
    {
        foreach (var box in boxes)
        {
            if (box.Contains(x, y))
                return true;
        }

        return false;
    }

    public static bool Contains(this IReadOnlyList<Box2i> boxes, Vector2i point)
    {
        foreach (var box in boxes)
        {
            if (box.Contains(point))
                return true;
        }

        return false;
    }

    //TODO: Standardize both of these methods to take either a StorageComp or EntityUid

    //TODO: Document
    //TODO: Figure out how to deal with someone passing a bad input
    //Scans a storage and all nested storages for items matching the condition.
    //Outputs a dictionary of <FoundItems, ContainingStorages>
    public static void ScanStorageForCondition(EntityUid storageItem,
        Predicate<EntityUid> condition,
        ref List<FoundItem> foundItemsAndContainers)
    {
        var entityManager = IoCManager.Resolve<IEntityManager>();
        entityManager.TryGetComponent<StorageComponent>(storageItem, out var storageComp);
        //Only way I know how to get this to compile
        if (storageComp == null)
        {
            //You done goofed up
            throw new ArgumentException("An object was passed to ScanStorageForCondition that did not have a storage component.");
        }

        foreach (var item in storageComp.StoredItems.Keys)
        {
            if (condition.Invoke(item))
                foundItemsAndContainers.Add(new FoundItem(item, storageItem));

            if (entityManager.TryGetComponent<StorageComponent>(item, out var storeComp))
                ScanStorageForCondition(item, condition, ref foundItemsAndContainers);
        }
    }

    //For use with ScanStorageForCondition()
    public struct FoundItem(EntityUid item, EntityUid container)
    {
        public EntityUid Item = item;
        public EntityUid Container = container;
    }
}
