import {
  GetParameterCommand,
  GetParameterHistoryCommand,
  PutParameterCommand,
  SSMClient,
  ServiceInputTypes,
  ServiceOutputTypes
} from '@aws-sdk/client-ssm';

import { SmithyResolvedConfiguration } from '@smithy/smithy-client';
import { Command, HttpHandlerOptions } from '@smithy/types';

export class SSMClientMock extends SSMClient {
  private _parameters = new Map<string, string[]>();

  override async send<InputType extends ServiceInputTypes, OutputType extends ServiceOutputTypes>(
    command:
      | Command<
          ServiceInputTypes,
          InputType,
          ServiceOutputTypes,
          OutputType,
          SmithyResolvedConfiguration<HttpHandlerOptions>
        >
      | any
  ): Promise<OutputType> {
    let result: OutputType | any = null;

    switch (command.constructor) {
      case PutParameterCommand: {
        const putCommand = command as PutParameterCommand;
        const history = this._parameters.get(putCommand.input.Name) || [];
        history.push(putCommand.input.Value);
        this._parameters.set(putCommand.input.Name, history);
        break;
      }
      case GetParameterCommand: {
        const getCommand = command as GetParameterCommand;
        const values = this._parameters.get(getCommand.input.Name) || [];
        result = {
          Parameter: {
            Value: values.length > 0 ? values[values.length - 1] : null
          }
        };
        break;
      }
      case GetParameterHistoryCommand: {
        const getCommand = command as GetParameterCommand;
        const values = this._parameters.get(getCommand.input.Name) || [];
        const params = values.map((v) => ({
          Value: v
        }));
        result = {
          Parameters: params
        };
        break;
      }
    }

    return result;
  }
}
