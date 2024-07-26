export class ArrayUtil {
  // SourceRef: https://advancedweb.hu/how-to-use-async-functions-with-array-filter-in-javascript/
  static async asyncFilter<T>(list: T[], predicate: (item: T) => Promise<boolean>): Promise<T[]> {
    const results = await Promise.all(list.map(predicate));

    return list.filter((_, index) => results[index]);
  }
}
