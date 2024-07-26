import _ from 'lodash';

export class ObjectUtil {
  static deepAssign<T>(final: any, ...remainder: T[]): T {
    // Replace arrays instead of trying to merge them.
    const result = _.mergeWith({}, final, ...remainder, (a: T, b: T) => (_.isArray(b) ? b : undefined));
    return result;
  }
}
