var ListItem = (function () {
    function ListItem(list, value, index) {
        this.list = list;
        this.index = index;
        this.value = value;
    }
    ListItem.prototype.prev = function () {
        return this.list.get(this.index - 1);
    };
    ListItem.prototype.next = function () {
        return this.list.get(this.index + 1);
    };
    return ListItem;
})();
var List = (function () {
    function List() {
        this.items = [];
    }
    List.prototype.size = function () {
        return this.items.length;
    };
    List.prototype.add = function (value) {
        this.items.push(new ListItem(this, value, this.size()));
    };
    List.prototype.get = function (index) {
        return this.items[index];
    };
    return List;
})();
